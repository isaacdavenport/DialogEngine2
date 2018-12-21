using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DialogGenerator.CharacterSelection.Helper
{
    public static class ParseMessageHelper
    {
        #region - Fields -

        public static List<ReceivedMessage> ReceivedMessages = new List<ReceivedMessage>();
        public static ILogger Logger;
        public static ICharacterRepository CharacterRepository;

        #endregion

        #region - Private methods -

        private static void _addMessageToReceivedBuffer(int _characterRowNum, int[] _rw, DateTime _timeStamp)
        {
            try
            {
                if (_characterRowNum > Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS).Count - 1)  
                {
                    return;
                }

                ReceivedMessages.Add(new ReceivedMessage()
                {
                    ReceivedTime = _timeStamp,
                    SequenceNum = _rw[ApplicationData.Instance.NumberOfRadios],
                    CharacterPrefix = CharacterRepository.GetByAssignedRadio(_characterRowNum).CharacterPrefix
                });

                //TODO add a lock around this
                for (int _i = 0; _i < ApplicationData.Instance.NumberOfRadios; _i++)
                {
                    ReceivedMessages.Last().Rssi[_i] = _rw[_i];
                }

                string _debugString = ReceivedMessages[ReceivedMessages.Count - 1].CharacterPrefix + "  ";

                for (var j = 0; j < ApplicationData.Instance.NumberOfRadios; j++)
                {
                    _debugString += ReceivedMessages[ReceivedMessages.Count - 1].Rssi[j].ToString("D3");
                    _debugString += " ";
                }

                _debugString += ReceivedMessages[ReceivedMessages.Count - 1].SequenceNum.ToString("D3");

                if((BLEDataProviderType)Session.Get<int>(Constants.BLE_DATA_PROVIDER) == BLEDataProviderType.Serial)
                {
                    Logger.Info(_debugString,ApplicationData.Instance.DecimalSerialDirectBLELoggerKey);
                }
                else
                {
                    Logger.Info(ApplicationData.Instance.DecimalSerialDirectBLELoggerKey, _debugString);
                }

                if (ReceivedMessages.Count > 30000)
                {
                    ReceivedMessages.RemoveRange(0, 100);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region - Public functions -


        public static void ProcessTheMessage(int _rowNum, int[] _newRow)
        {
            try
            {
                for (int _k = 0; _k < ApplicationData.Instance.NumberOfRadios; _k++)
                {
                    if (CharacterRepository.GetByAssignedRadio(_rowNum) != null)
                    {
                        BLESelectionService.HeatMap[_rowNum, _k] = _newRow[_k];
                    }
                    else
                    {
                        BLESelectionService.HeatMap[_rowNum, _k] = 0;
                    }
                }

                var _currentDateTime = DateTime.Now;

                BLESelectionService.CharactersLastHeatMapUpdateTime[_rowNum] = _currentDateTime;

                _addMessageToReceivedBuffer(_rowNum, _newRow, _currentDateTime);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public static int ParseBle(string _message, ref int[] _rssiRow)
        {
            try
            {
                // rssiRow also has seqNum from FW at end
                int _rowNumber = -1;

                if (_message.IndexOf("a5",StringComparison.OrdinalIgnoreCase) < 0)
                    return _rowNumber;

                if ((BLEDataProviderType)Session.Get<int>(Constants.BLE_DATA_PROVIDER) == BLEDataProviderType.WinBLEWatcher)
                {
                    string[] parts = _message.Split('-');

                    if (parts.Count() < ApplicationData.Instance.NumberOfRadios)
                        return _rowNumber;

                    for(int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                    {
                        _rssiRow[i] = int.Parse(parts[i], System.Globalization.NumberStyles.HexNumber);

                        if (_rssiRow[i] == 0xFF) _rowNumber = i;
                    }

                    _rssiRow[ApplicationData.Instance.NumberOfRadios] = int.Parse(parts.Last(), System.Globalization.NumberStyles.HexNumber);
                }
                else
                {    // we should not be in here anymore unless we have gone back to the USB-Serial CSR dongle because internal BLE laptop HW didn't work
                    if (_message.Length != 19 || !_message.StartsWith("ff"))
                        return _rowNumber;

                    for (int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                    {
                        string _subMessage = _message.Substring(i * 2 + 2, 2);
                        _rssiRow[i] = int.Parse(_subMessage, System.Globalization.NumberStyles.HexNumber);
                        if (_rssiRow[i] == 0xFF) _rowNumber = i;
                    }

                    // The final int after the receiver for the PC, skipping "a5" key value is sequence number
                    _rssiRow[ApplicationData.Instance.NumberOfRadios] = int.Parse(_message.Substring(ApplicationData.Instance.NumberOfRadios * 2 + 4, 2), System.Globalization.NumberStyles.HexNumber);
                }

                if (_rowNumber == -1 && ApplicationData.Instance.MonitorMessageParseFails)
                    Logger.Error("Failed to parse message.");

                return _rowNumber;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #endregion
    }
}
