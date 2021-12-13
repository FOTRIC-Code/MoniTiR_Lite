// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;

using Newtonsoft.Json;

namespace Firefly.Client.Control.Fire
{
    public class GeneralViewModel : BaseViewModel
    {
        #region Fields

        private float sampleInterval;
        private bool dataRecordEnabled;
        private int dataRecordInterval;
        private bool imageRecordEnabled;
        private int imageRecordInterval;
        private bool fixedMonitoring;
        private bool enableSuperResolution​​;

        #endregion

        #region Constructors

        public GeneralViewModel()
        {
            sampleInterval = 10;
            dataRecordEnabled = true;
            dataRecordInterval = 20;
            imageRecordEnabled = true;
            imageRecordInterval = 50;
            FixedMonitoring = true;
            EnableSuperResolution​​ = false;
        }

        #endregion

        #region Properties

        public Single SampleInterval
        {
            get => sampleInterval;
            set
            {
                sampleInterval = value;
                OnPropertyChanged(nameof(SampleInterval));
            }
        }

        public bool DataRecordEnabled
        {
            get => dataRecordEnabled;
            set
            {
                dataRecordEnabled = value;
                if (!value)
                {
                    ImageRecordEnabled = false;
                }

                OnPropertyChanged(nameof(DataRecordEnabled));
            }
        }

        public int DataRecordInterval
        {
            get => dataRecordInterval;
            set
            {
                dataRecordInterval = value;
                OnPropertyChanged(nameof(DataRecordInterval));
            }
        }

        public bool ImageRecordEnabled
        {
            get => imageRecordEnabled;
            set
            {
                imageRecordEnabled = value;

                OnPropertyChanged(nameof(ImageRecordEnabled));
            }
        }

        public int ImageRecordInterval
        {
            get => imageRecordInterval;
            set
            {
                imageRecordInterval = value;
                OnPropertyChanged(nameof(ImageRecordInterval));
            }
        }

        public bool FixedMonitoring
        {
            get => fixedMonitoring;
            set
            {
                fixedMonitoring = value;
                OnPropertyChanged(nameof(FixedMonitoring));
            }
        }

        public bool EnableSuperResolution​​
        {
            get => enableSuperResolution​​;
            set
            {
                enableSuperResolution​​ = value;
                OnPropertyChanged(nameof(EnableSuperResolution​​));
            }
        }

        #endregion

        #region Methods

        public void Load(GeneralConfigDataObject configDO)
        {
            SampleInterval = configDO.SampleInterval;
            DataRecordEnabled = configDO.DataRecordEnabled;
            DataRecordInterval = configDO.DataRecordInterval;
            ImageRecordEnabled = configDO.ImageRecordEnabled;
            ImageRecordInterval = configDO.ImageRecordInterval;
            FixedMonitoring = configDO.FixedMonitoring;
            EnableSuperResolution​​ = configDO.EnableSuperResolution;
        }

        public GeneralConfigDataObject ConvertToDataObject()
        {
            return new GeneralConfigDataObject
            {
                SampleInterval = SampleInterval,
                DataRecordEnabled = DataRecordEnabled,
                DataRecordInterval = DataRecordInterval,
                ImageRecordEnabled = ImageRecordEnabled,
                ImageRecordInterval = ImageRecordInterval,
                FixedMonitoring = FixedMonitoring,
                EnableSuperResolution​​ = EnableSuperResolution​​,
            };
        }

        public string ConvertToJson()
        {
            return JsonConvert.SerializeObject(new FireAgentDataObject(){GeneralConfig = ConvertToDataObject()}, Formatting.Indented);
        }

        #endregion
    }
}
