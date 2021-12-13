// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using FFmpeg.AutoGen;
using log4net;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Firefly.Client.Control
{
    public interface IRTSPClientCallback
    {
        void OnConnectChanged(bool isConnected);

        void OnFrameChanged(IntPtr data, bool isKeyFrame, int width, int height);

        void OnRecordStatusChanged(bool record);

        void OnRecordFinished(string recordPath);
    }

    public unsafe class RTSPClient : IRTSPConnectionCallback, IVideoFrameCallback
    {
        #region Fields

        private const int DefaultWaitMilliseconds = 3 * 1000;

        private IRTSPClientCallback _rtspClientCallback;

        private VideoStreamDecoder _streamDecoder;
        private object _streamDecoderObj = new object();

        private long _hasUrl = 0;
        private string _url;

        private ILog _logger = LogManager.GetLogger(typeof(RTSPClient));

        /// <summary>
        /// 0 disConnect, 1 startConnect, 2 connected
        /// </summary>
        private long _isConnected = 0;

        #endregion Fields

        #region Constructors

        static RTSPClient()
        {
            string currentDirectory = Environment.CurrentDirectory;
            // Prefer flat ./FFmpeg (same as Firefly.Stream.Core); fall back to legacy FFmpeg\x64|x86.
            string ffmpegBinaryPath = Path.Combine(currentDirectory, "FFmpeg");
            if (!Directory.Exists(ffmpegBinaryPath))
            {
                ffmpegBinaryPath = Path.Combine(currentDirectory, Path.Combine("FFmpeg", Environment.Is64BitProcess ? "x64" : "x86"));
            }

            if (Directory.Exists(ffmpegBinaryPath))
            {
                ffmpeg.RootPath = ffmpegBinaryPath;
            }
        }
        #endregion Constructors

        public bool IsConnected => Interlocked.Read(ref _isConnected) == 2;
        public bool IsConnecting => Interlocked.Read(ref _isConnected) == 1;

        #region public methods

        public void Register(string url, IRTSPClientCallback clientCallback)
        {
            _rtspClientCallback = clientCallback;
            if (string.IsNullOrWhiteSpace(url))
            {
                Interlocked.Exchange(ref _hasUrl, 0);
            }
            else
            {
                Interlocked.Exchange(ref _hasUrl, 1);
            }
            _url = url;

            StartPlay();
        }

        public void Destroy()
        {
            Interlocked.Exchange(ref _hasUrl, 0);
            _url = string.Empty;
            StopPlay();
        }

        public void StartRecord(string videoPath)
        {
            if (_streamDecoder != null)
            {
                if (!_streamDecoder.StartRecord(videoPath))
                {
                    _rtspClientCallback?.OnRecordStatusChanged(false);
                }
            }
        }

        public void StopRecord()
        {
            _streamDecoder?.StopRecord();
        }
        #endregion public methods

        #region private methods

        private void StartPlay()
        {
            Interlocked.Exchange(ref _isConnected, 1);
            _rtspClientCallback?.OnConnectChanged(true);

            VideoStreamDecoder decoder = new VideoStreamDecoder(_url, this);

            lock (_streamDecoderObj)
            {
                _streamDecoder = decoder;
                _streamDecoder.VideoFrameCallback = this;
                _streamDecoder.StartPlay();
            }
        }

        private void StopPlay()
        {
            lock (_streamDecoderObj)
            {
                if (_streamDecoder != null)
                {
                    _streamDecoder?.StopPlay();
                    _streamDecoder = null;
                }
            }

            _rtspClientCallback = null;
        }

        public void ConnectedFailed(bool isManual)
        {
            if (IsConnected || IsConnecting)
            {
                Interlocked.Exchange(ref _isConnected, 0);
                _rtspClientCallback?.OnConnectChanged(false);
            }
        }

        public void OnRecordStarted()
        {
            _rtspClientCallback?.OnRecordStatusChanged(true);
        }

        public void OnRecordFinished(string recordPath)
        {
            _rtspClientCallback?.OnRecordFinished(recordPath);
        }

        public void OnRenderVideoFrame(AVFrame* avFrame, bool isKeyFrame, int width, int height)
        {
            if (!IsConnected)
            {
                Interlocked.Exchange(ref _isConnected, 2);
                _rtspClientCallback?.OnConnectChanged(true);
            }

            _rtspClientCallback?.OnFrameChanged((IntPtr)avFrame->data[0], isKeyFrame, avFrame->width, avFrame->height);
        }

        #endregion private methods
    }
}