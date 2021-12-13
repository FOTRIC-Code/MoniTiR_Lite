// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using FFmpeg.AutoGen;
using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Firefly.Client.Control
{
    internal unsafe interface IVideoFrameCallback
    {
        void OnRenderVideoFrame(AVFrame* avFrame, bool isKeyFrame, int width, int height);
    }

    internal interface IRTSPConnectionCallback
    {
        void ConnectedFailed(bool isManual);

        void OnRecordStarted();

        void OnRecordFinished(string recordPath);
    }

    internal sealed unsafe class VideoStreamDecoder
    {
        #region Fields

        private const int DECODE_ERROR_MAXTIMES = 10;// 允许连续解码错误的最大次数
        private const int FRAMES_PER_FILE = 250 * 6; // 1分钟一个文件
        private AVFormatContext* _inContext; //输入封装
        private AVStream* _inStream; //输入流
        private AVCodecContext* _codecCtxH264; //输入流相关编码数据
        private AVCodec* _codecH264; //解码器
        private AVBufferRef* _hwDeviceCtx; //硬件解码器
        private AVPacket* _pktH264;
        private AVFrame* _pFrame;
        private AVFrame* _receivedFrame;
        private AVFrame* _convertedFrame;
        private int _streamIndex = 0;
        private bool _disposed = false;

        private AVFormatContext* _outContext;
        private AVCodecContext* _outCodecCtxH264;
        private int _frameCount = 0;
        private AVPacket* _outAVPacket;
        private AVFrame* _outConvertedFrame;

        private SwsContext* _inConvertContext;
        private SwsContext* _outConvertContext;

        private string _url;
        private string _recordFileName;

        private long _isRun = 0;
        private long _isRunning = 0;

        private Thread _playThread;
        private int _decodeErrorCount = 0;
        private DateTime _lastFrameTime;

        /// <summary>
        /// 0 stopped, 1 recording, 2 stopping
        /// </summary>
        private long _isRecord = 0;

        private ILog _logger = LogManager.GetLogger(typeof(VideoStreamDecoder));

        #endregion Fields

        #region Constructors

        internal VideoStreamDecoder(string url, IRTSPConnectionCallback videoStreamCallback)
        {
            _url = url;
            VideoConnectionCallback = videoStreamCallback;
        }

        #endregion Constructors

        #region Properties

        internal IRTSPConnectionCallback VideoConnectionCallback { get; set; }
        internal IVideoFrameCallback VideoFrameCallback { get; set; }

        private bool _interlockedRun => Interlocked.Read(ref _isRun) == 1;

        private bool _interlockedRecord => Interlocked.Read(ref _isRecord) == 1;

        #endregion Properties

        #region Methods

        #region Input
        public void StartPlay()
        {
            Interlocked.Exchange(ref _isRun, 1);

            _pktH264 = ffmpeg.av_packet_alloc();
            _pFrame = ffmpeg.av_frame_alloc();

            int error;
            _playThread = new Thread(() =>
            {
                Init();
                while (_interlockedRun)
                {
                    Interlocked.Exchange(ref _isRunning, 1);
                    try
                    {
                        //获取包
                        //当网络断线时，会阻塞10s，返回-138（ETIMEOUT）且网络恢复后无法恢复
                        if (_inContext == null)
                        {
                            CheckVideoConnectionCallback();
                            continue;
                        }

                        error = ffmpeg.av_read_frame(_inContext, _pktH264);
                        if (error < 0)
                        {
                            CheckVideoConnectionCallback();
                            continue;
                        }

                        if (_pktH264->stream_index == _streamIndex)
                        {
                            //发送包给解码器
                            error = ffmpeg.avcodec_send_packet(_codecCtxH264, _pktH264);
                            if (error < 0)
                            {
                                _logger.Warn("EINVAL : Codec Error");
                            }
                        }
                        else
                        {
                            ffmpeg.av_packet_unref(_pktH264);
                        }
                        while (true)
                        {
                            //从包解帧
                            error = ffmpeg.avcodec_receive_frame(_codecCtxH264, _pFrame);

                            if (error < 0)
                            {
                                if (error == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                                {
                                    ffmpeg.av_packet_unref(_pktH264);
                                    break;
                                }

                                //Interlocked.Exchange(ref _isRun, 0);

                                if (CheckVideoConnectionCallback())
                                {
                                    break;
                                }
                            }
                            else
                            {
                                _decodeErrorCount = 0; //解码成功，重置计数器

                                if (_codecCtxH264->hw_device_ctx != null)
                                {
                                    ffmpeg.av_hwframe_transfer_data(_receivedFrame, _pFrame, 0);
                                    //qsv解出来的帧是nv12,nvevc是啥？？应该也是nv12

                                    if (_interlockedRecord)
                                    {
                                        WriteFrame(_receivedFrame);
                                    }
                                    AVFrame* frame = TryConvertFrame(_receivedFrame);
                                    VideoFrameCallback?.OnRenderVideoFrame(frame, _receivedFrame->key_frame == 1, frame->width, frame->height);
                                    ffmpeg.av_frame_unref(_receivedFrame);
                                }
                                else
                                {
                                    //这里解出来的帧是yuv420p
                                    if (_interlockedRecord)
                                    {
                                        WriteFrame(_pFrame);
                                    }
                                    AVFrame* frame = TryConvertFrame(_pFrame);
                                    VideoFrameCallback?.OnRenderVideoFrame(frame, _pFrame->key_frame == 1, frame->width, frame->height);
                                    ffmpeg.av_frame_unref(_pFrame);
                                }

                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isRunning, 0);
                    }
                }
            });
            _playThread.Name = "PlayThread" + _url;
            _playThread.Start();

        }

        public void StopPlay()
        {
            if (_playThread != null)
            {
                StopRecord();

                Interlocked.Exchange(ref _isRun, 0);

                while (Interlocked.Read(ref _isRunning) == 1)
                {
                    Thread.Sleep(10);
                }

                _playThread.Join();
                _playThread = null;
            }

            if (_pFrame != IntPtr.Zero.ToPointer())
            {
                fixed (AVFrame** frame = &_pFrame)
                {
                    ffmpeg.av_frame_free(frame);
                }
            }

            if (_pktH264 != IntPtr.Zero.ToPointer())
            {
                fixed (AVPacket** pkt = &_pktH264)
                {
                    ffmpeg.av_packet_free(pkt);
                }
            }

            DeInit();
            VideoFrameCallback = null;
        }

        private void Init()
        {
            try
            {
                if (StreamConnection(_url))
                {
                    if (InitInStream())
                    {
                        if (!InitH264Codec())
                        {
                            _logger.Warn($"Init H264Codec fail. {_url}");
                            VideoConnectionCallback?.ConnectedFailed(false);
                        }
                    }
                    else
                    {
                        _logger.Warn($"Init OutStream fail. {_url}");
                        VideoConnectionCallback?.ConnectedFailed(false);
                    }
                }
                else
                {
                    _logger.Warn($"StreamConnection fail. {_url}");
                    VideoConnectionCallback?.ConnectedFailed(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"VideoStreamDecoder Init Exception: {ex.ToString()}");
            }
        }

        private void DeInit()
        {
            if (_receivedFrame != IntPtr.Zero.ToPointer())
            {
                fixed (AVFrame** frame = &_receivedFrame)
                {
                    ffmpeg.av_frame_free(frame);
                }
            }

            if (_convertedFrame != IntPtr.Zero.ToPointer())
            {
                fixed (AVFrame** frame = &_convertedFrame)
                {
                    ffmpeg.av_frame_free(frame);
                }
            }

            if (_inConvertContext != IntPtr.Zero.ToPointer())
            {
                ffmpeg.sws_freeContext(_inConvertContext);
                _inConvertContext = null;
            }

            if (_codecCtxH264 != IntPtr.Zero.ToPointer())
            {
                ffmpeg.avcodec_close(_codecCtxH264);
                if (_hwDeviceCtx != IntPtr.Zero.ToPointer())
                {
                    fixed (AVBufferRef** deviceCtx = &_hwDeviceCtx)
                    {
                        ffmpeg.av_buffer_unref(deviceCtx);
                    }
                }

                fixed (AVCodecContext** codec = &_codecCtxH264)
                {
                    ffmpeg.avcodec_free_context(codec);
                }
            }

            if (_inContext != IntPtr.Zero.ToPointer())
            {
                fixed (AVFormatContext** ctx = &_inContext)
                {
                    ffmpeg.avformat_close_input(ctx);
                    _inContext = null;
                }
            }
        }

        private bool CheckVideoConnectionCallback()
        {
            _decodeErrorCount++;
            if (_decodeErrorCount > DECODE_ERROR_MAXTIMES)
            {
                DeInit();
                Init();

                VideoConnectionCallback?.ConnectedFailed(false);
                _decodeErrorCount = 0;
                return true;
            }
            return false;
        }

        private bool StreamConnection(string url)
        {
            AVDictionary* opts = null;
            ffmpeg.av_dict_set(&opts, "rtsp_transport", "tcp", 0);
            ffmpeg.av_dict_set(&opts, "timeout", "10000000", 0);
            AVFormatContext* inContext;
            //调用avop_open2()建立连接 构造并初始化urlContext
            //av_probe_input_buffer2()接收数据探测格式 初始化avStream
            int re = ffmpeg.avformat_open_input(&inContext, url, null, &opts);
            if (re < 0)
            {
                return false;
            }

            _inContext = inContext;
            return re >= 0;
        }

        private bool InitInStream()
        {
            for (int i = 0; i < _inContext->nb_streams; i++)
            {
                if (_inContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    _inStream = _inContext->streams[i];
                    return true;
                }
            }
            return false;
        }

        private bool FindNVENC()
        {
            AVBufferRef* ctx = null;
            _codecH264 = ffmpeg.avcodec_find_decoder_by_name("h264_nvenc");
            if (_codecH264 == IntPtr.Zero.ToPointer())
            {
                _logger.Warn("Fail: h264_nvenc not found.");
                return false;
            }

            int ret = ffmpeg.av_hwdevice_ctx_create(&ctx, AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA, "auto", null, 0);
            if (ret < 0)
            {
                _logger.Warn("Fail: h264_nvenc not create.");
                return false;
            }

            _hwDeviceCtx = ctx;
            return true;
        }

        private bool FindQSV()
        {
            AVBufferRef* ctx = null;
            _codecH264 = ffmpeg.avcodec_find_decoder_by_name("h264_qsv");
            if (_codecH264 == IntPtr.Zero.ToPointer())
            {
                _logger.Warn("Fail: h264_nvenc not found.");
                return false;
            }

            int ret = ffmpeg.av_hwdevice_ctx_create(&ctx, AVHWDeviceType.AV_HWDEVICE_TYPE_QSV, "auto", null, 0);
            if (ret < 0)
            {
                _logger.Warn("Fail: h264_qsv not create.");
                return false;
            }

            _hwDeviceCtx = ctx;
            return true;
        }

        private bool FindAuto()
        {
            int error = ffmpeg.avformat_find_stream_info(_inContext, null);
            if (error < 0)
            {
                _logger.Warn("Fail: avformat_find_stream_info.");
                return false;
            }
            AVCodec* codec = null;
            _streamIndex = ffmpeg.av_find_best_stream(_inContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
            _codecH264 = codec;

            if (_codecH264 == IntPtr.Zero.ToPointer())
            {
                _logger.Warn("Fail: h264 encoder not found.");
                return false;
            }
            _inStream = _inContext->streams[_streamIndex];
            return true;
        }

        private bool InitH264Codec()
        {
            if (!FindAuto())
            {
                _logger.Warn("Fail: find decoder.");
                return false;
            }

            _codecCtxH264 = ffmpeg.avcodec_alloc_context3(_codecH264);
            if (_codecCtxH264 == IntPtr.Zero.ToPointer())
            {
                _logger.Warn("Fail: avcodec_alloc_context3.");
                return false;
            }

            if (_hwDeviceCtx != IntPtr.Zero.ToPointer())
            {
                _codecCtxH264->hw_device_ctx = ffmpeg.av_buffer_ref(_hwDeviceCtx);
            }

            //这里复制流的参数到编码器上下文
            if (ffmpeg.avcodec_parameters_to_context(_codecCtxH264, _inStream->codecpar) < 0)
            {
                _logger.Warn("Fail: avcodec_parameters_to_context.");
                return false;
            }

            //打开解码器
            //解码器是独立的，直接打开，后面send_pkt给解码器，就可以了
            if (ffmpeg.avcodec_open2(_codecCtxH264, _codecH264, null) < 0)
            {
                _logger.Warn("Fail: avcodec_open2.");
                return false;
            }

            return true;
        }

        public AVFrame* TryConvertFrame(AVFrame* frame)
        {
            if (_convertedFrame == IntPtr.Zero.ToPointer())
            {
                _convertedFrame = ffmpeg.av_frame_alloc();
                _convertedFrame->width = frame->width;
                _convertedFrame->height = frame->height;
                _convertedFrame->format = (int)AVPixelFormat.AV_PIX_FMT_BGR24;

                ffmpeg.av_frame_get_buffer(_convertedFrame, 1);
            }
            if (_inConvertContext == IntPtr.Zero.ToPointer())
            {
                _inConvertContext = ffmpeg.sws_getContext(_codecCtxH264->width, _codecCtxH264->height, _codecCtxH264->pix_fmt, _convertedFrame->width, _convertedFrame->height, AVPixelFormat.AV_PIX_FMT_BGR24, ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            }

            if (_inConvertContext != IntPtr.Zero.ToPointer())
            {
                ffmpeg.sws_scale(_inConvertContext, frame->data, frame->linesize, 0, frame->height, _convertedFrame->data, _convertedFrame->linesize);
                return _convertedFrame;
            }

            return null;
        }

        #endregion Input

        #region output

        public bool StartRecord(string fileName)
        {
            if (Interlocked.Read(ref _isRecord) == 2)
            {
                return false;
            }
            _outAVPacket = ffmpeg.av_packet_alloc();
            return CreateNewFile(fileName);
        }

        public void StopRecord()
        {
            Interlocked.Exchange(ref _isRecord, 2);
            if (_outContext != IntPtr.Zero.ToPointer())
            {
                int ret = ffmpeg.av_write_trailer(_outContext);
                if (ret < 0)
                {
                    _logger.Warn($"Error writing output file trailer: {ret}");
                }

                if (_outAVPacket != IntPtr.Zero.ToPointer())
                {
                    fixed (AVPacket** pkt = &_outAVPacket)
                    {
                        ffmpeg.av_packet_free(pkt);
                    }
                }

                if (_outConvertContext != IntPtr.Zero.ToPointer())
                {
                    ffmpeg.sws_freeContext(_outConvertContext);
                    _outConvertContext = null;
                }

                if (_outCodecCtxH264 != IntPtr.Zero.ToPointer())
                {
                    ffmpeg.avcodec_close(_outCodecCtxH264);
                    fixed (AVCodecContext** codec = &_outCodecCtxH264)
                    {
                        ffmpeg.avcodec_free_context(codec);
                    }
                }

                fixed (AVFormatContext** ctx = &_outContext)
                {
                    ffmpeg.avio_close(_outContext->pb);
                    ffmpeg.avformat_free_context(_outContext);
                    _outContext = null;
                }
            }
            Interlocked.Exchange(ref _isRecord, 0);
            VideoConnectionCallback?.OnRecordFinished(_recordFileName);
            _recordFileName = string.Empty;
        }

        private bool CreateNewFile(string fileName)
        {
            AVFormatContext* outputFormatContext = null;
            int ret = ffmpeg.avformat_alloc_output_context2(&outputFormatContext, null, "mp4", fileName);
            if (ret < 0)
            {
                _logger.Warn($"Error creating output context: {ret}");
                return false;
            }

            _outContext = outputFormatContext;
            AVCodec* codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MPEG4);
            _outCodecCtxH264 = ffmpeg.avcodec_alloc_context3(codec);
            _outCodecCtxH264->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            _outCodecCtxH264->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
            _outCodecCtxH264->width = _codecCtxH264->width;
            _outCodecCtxH264->height = _codecCtxH264->height;
            _outCodecCtxH264->time_base = new AVRational() { num = 1, den = 25 };
            _outCodecCtxH264->framerate = new AVRational() { num = 25, den = 1 };
            _outCodecCtxH264->gop_size = 10;
            _outCodecCtxH264->max_b_frames = 1;
            _outCodecCtxH264->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;

            ret = ffmpeg.avcodec_open2(_outCodecCtxH264, codec, null);
            if (ret < 0)
            {
                _logger.Warn($"Error opening codec: {ret}");
                return false;
            }

            ret = ffmpeg.avio_open2(&outputFormatContext->pb, fileName, ffmpeg.AVIO_FLAG_WRITE, null, null);
            if (ret < 0)
            {
                _logger.Warn($"Error opening output file: {ret}");
                return false;
            }

            var outputStream = ffmpeg.avformat_new_stream(_outContext, codec);
            if (outputStream == IntPtr.Zero.ToPointer())
            {
                _logger.Warn("Error creating output stream.");
                return false;
            }

            outputStream->time_base = new AVRational() { num = 1, den = 25 };
            outputStream->id = (int)(outputFormatContext->nb_streams - 1);
            outputStream->codecpar->codec_tag = 0;
            ret = ffmpeg.avcodec_parameters_from_context(outputStream->codecpar, _outCodecCtxH264);
            if (ret < 0)
            {
                _logger.Warn($"Error opening output file: {ret}");
                return false;
            }

            ret = ffmpeg.avformat_write_header(outputFormatContext, null);
            if (ret < 0)
            {
                _logger.Warn($"Error writing output file header: {ret}");
                return false;
            }

            if (_outConvertContext == IntPtr.Zero.ToPointer())
            {
                _outConvertContext = ffmpeg.sws_getContext(_codecCtxH264->width, _codecCtxH264->height, _codecCtxH264->pix_fmt, _codecCtxH264->width, _codecCtxH264->height, AVPixelFormat.AV_PIX_FMT_YUV420P, ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            }

            if (_outConvertContext == IntPtr.Zero.ToPointer())
            {
                _logger.Warn($"Error get swsContext");
                return false;
            }

            if (_outConvertedFrame == IntPtr.Zero.ToPointer())
            {
                _outConvertedFrame = ffmpeg.av_frame_alloc();
                _outConvertedFrame->width = _codecCtxH264->width;
                _outConvertedFrame->height = _codecCtxH264->height;
                _outConvertedFrame->format = (int)AVPixelFormat.AV_PIX_FMT_YUV420P;
                ffmpeg.av_frame_get_buffer(_outConvertedFrame, 0);
            }

            _frameCount = 0;

            _recordFileName = fileName;
            Interlocked.Exchange(ref _isRecord, 1);
            VideoConnectionCallback?.OnRecordStarted();
            return true;
        }

        private bool WriteFrame(AVFrame* avFrame)
        {
            if (_outConvertContext == IntPtr.Zero.ToPointer())
            {
                _logger.Warn($"Error _outConvertContext");
                return false;
            }

            ffmpeg.sws_scale(_outConvertContext, avFrame->data, avFrame->linesize, 0, avFrame->height, _outConvertedFrame->data, _outConvertedFrame->linesize);

            _outConvertedFrame->pts = _frameCount;
            int ret = ffmpeg.avcodec_send_frame(_outCodecCtxH264, _outConvertedFrame);

            while (true)
            {
                ret = ffmpeg.avcodec_receive_packet(_outCodecCtxH264, _outAVPacket);

                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                {
                    break;
                }

                if (ret < 0)
                {
                    _logger.Warn($"Error during encoding {ret}");
                    return false;
                }

                long calc_duration = 512; //12800/25
                _outAVPacket->pts = _frameCount * calc_duration;
                _outAVPacket->dts = _outAVPacket->pts;
                _outAVPacket->duration = calc_duration;
                // _outAVPacket->stream_index = _streamIndex;
                ret = ffmpeg.av_interleaved_write_frame(_outContext, _outAVPacket);
                if (ret < 0)
                {
                    _logger.Warn($"Error writing frame: {ret}");
                    return false;
                }

                ffmpeg.av_packet_unref(_outAVPacket);
            }

            _frameCount++;

            return true;
        }

        #endregion output


        #endregion Methods
    }
}