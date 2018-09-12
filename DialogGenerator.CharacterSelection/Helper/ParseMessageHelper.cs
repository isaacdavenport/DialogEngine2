using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
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

        #endregion

        #region - Private methods -

        private static void _addMessageToReceivedBuffer(int _characterRowNum, int[] _rw, DateTime _timeStamp)
        {
            try
            {
                if (_characterRowNum > Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS).Count - 1)  //was omiting character 5 from log when it was Count - 2
                {
                    return;
                }

                ReceivedMessages.Add(new ReceivedMessage()
                {
                    ReceivedTime = _timeStamp,
                    SequenceNum = _rw[ApplicationData.Instance.NumberOfRadios],
                    CharacterPrefix = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS)[_characterRowNum].CharacterPrefix
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

                //TODO uncomment
                //LoggerHelper.Info(ApplicationData.Instance.DecimaSeriallLoggerKey, _debugString);

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


        public static void ProcessMessage(int _rowNum, int[] _newRow)
        {
            try
            {
                for (int _k = 0; _k < ApplicationData.Instance.NumberOfRadios; _k++)
                {
                    if (Session.Get<Dictionary<int,Character>>(Constants.CH_RADIO_RELATIONSHIP)[_rowNum]!= null)
                    {
                        SerialSelectionService.HeatMap[_rowNum, _k] = _newRow[_k];
                    }
                    else
                    {
                        SerialSelectionService.HeatMap[_rowNum, _k] = 0;
                    }
                }

                var _currentDateTime = DateTime.Now;

                SerialSelectionService.CharactersLastHeatMapUpdateTime[_rowNum] = _currentDateTime;

                _addMessageToReceivedBuffer(_rowNum, _newRow, _currentDateTime);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public static int Parse(string _message, ref int[] _rssiRow)
        {
            try
            {
                // rssiRow also has seqNum from FW at end
                int _rowNumber = -1;

                if (_message.StartsWith("ff") && _message.Contains("a5") && _message.Length == 19)
                {
                    for (int _i = 0; _i < ApplicationData.Instance.NumberOfRadios; _i++)
                    {
                        string _subMessage = _message.Substring(_i * 2 + 2, 2);
                        _rssiRow[_i] = int.Parse(_subMessage, System.Globalization.NumberStyles.HexNumber);
                        if (_rssiRow[_i] == 0xFF) _rowNumber = _i;
                    }

                    // The final int after the receiver for the PC, skipping "a5" key value is sequence number
                    _rssiRow[ApplicationData.Instance.NumberOfRadios] = int.Parse(_message.Substring(ApplicationData.Instance.NumberOfRadios * 2 + 4, 2), System.Globalization.NumberStyles.HexNumber);

                }

                //TODO uncomment
                //if (_rowNumber == -1 && ApplicationData.Instance.MonitorMessageParseFails)
                //    mLogger.Error("Failed to parse message.");

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
