using DialogGenerator.CharacterSelection.Model;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DialogGenerator.CharacterSelection.Data;

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

        private static void _addMessageToReceivedBuffer(int _characterRowNum, BLE_Message _rw, DateTime _timeStamp)
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
                    Motion =      _rw.msgArray[_rw.msgArray.Length - 2],
                    SequenceNum = _rw.msgArray[_rw.msgArray.Length - 1],
                    CharacterPrefix = CharacterRepository.GetByAssignedRadio(_characterRowNum).CharacterPrefix
                });

                //TODO add a lock around this
                for (int _i = 0; _i < ApplicationData.Instance.NumberOfRadios; _i++)
                {
                    ReceivedMessages.Last().Rssi[_i] = _rw.msgArray[_i];
                }

                string _debugString = ReceivedMessages[ReceivedMessages.Count - 1].CharacterPrefix + "  ";

                for (var j = 0; j < ApplicationData.Instance.NumberOfRadios; j++)
                {
                    _debugString += ReceivedMessages[ReceivedMessages.Count - 1].Rssi[j].ToString("D3");
                    _debugString += " ";
                }

                _debugString += ReceivedMessages[ReceivedMessages.Count - 1].Motion.ToString("D3") + " ";
                _debugString += ReceivedMessages[ReceivedMessages.Count - 1].SequenceNum.ToString("D3");
                Logger.Info(_debugString, ApplicationData.Instance.DecimalSerialDirectBLELoggerKey);

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


        public static void ProcessTheMessage(int _rowNum, BLE_Message _newRow)
        {
            try
            {
                for (int _k = 0; _k < ApplicationData.Instance.NumberOfRadios; _k++)
                {
                    if (CharacterRepository.GetByAssignedRadio(_rowNum) != null)
                    {
                        BLESelectionService.HeatMap[_rowNum, _k] = _newRow.msgArray[_k];
                    }
                    else
                    {
                        BLESelectionService.HeatMap[_rowNum, _k] = 0;
                    }
                }

                var _currentDateTime = DateTime.Now;

                BLESelectionService.CharactersLastHeatMapUpdateTime[_rowNum] = _currentDateTime;
                BLESelectionService.MotionVector[_rowNum] = _newRow.msgArray[ApplicationData.Instance.NumberOfRadios];

                _addMessageToReceivedBuffer(_rowNum, _newRow, _currentDateTime);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static int ParseBle(BLE_Message _message, BLE_Message _rssiRow)
        {
            try
            {
                // rssiRow also has seqNum from FW at end
                int _rowNumber = -1;

                for (int i = 0; i < ApplicationData.Instance.NumberOfRadios; i++)
                {
                    _rssiRow.msgArray[i] = _message.msgArray[i];

                    if (_rssiRow.msgArray[i] == 0xFF)
                        _rowNumber = i;
                }

                _rssiRow.msgArray[_rssiRow.msgArray.Length - 2] = _message.msgArray[_message.msgArray.Length - 2];   // motion byte
                _rssiRow.msgArray[_rssiRow.msgArray.Length - 1] = _message.msgArray[_message.msgArray.Length - 1];   //  sequence number

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
