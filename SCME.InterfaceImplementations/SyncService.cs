using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.Interfaces;
using System.ComponentModel;

namespace SCME.InterfaceImplementations
{
    public class SyncService : ISyncService
    {
        private readonly string _mmeCode;
        private readonly SQLiteConnection _connection;
        private readonly IProfilesService _saveProfileService;
        private readonly IResultsService _resultsService;

        private AfterSyncResultsRoutine FAfterSyncResultsRoutine;
        private AfterSyncProfilesRoutine FAfterSyncProfilesRoutine;
        
        public SyncService(string databasePath, string mmeCode)
        {
            _mmeCode = mmeCode;
            _connection = new SQLiteConnection(databasePath, false);
            _connection.Open();

            _saveProfileService = new SQLiteProfilesService(databasePath);
            _resultsService = new SQLiteResultsServiceLocal(databasePath);
        }

        private void SyncResultsWorkerHandler(DoWorkEventArgs workerParameters)
        {
            try
            {
                var unsendedDevices = _resultsService.GetUnsendedDevices();
                using (var centralDbClient = new CentralDatabaseServiceClient())
                {
                    foreach (var unsendedDevice in unsendedDevices)
                    {
                        var sended = centralDbClient.SendResultToServer(unsendedDevice);
                        if (sended)
                            _resultsService.SetResultSended(unsendedDevice.Id);
                    }
                }
            }
            catch (Exception ex)
            {               
                throw new Exception(string.Format("Error while syncing results from local database with a master:{0}", ex.Message));
            }
        }

        private void SyncResultsCompletedHandler(string error)
        {
            //этот код выполняется в потоке диспетчера, поэтому как обычно обращаемся к разделяемым данным и пользовательскому интерфейсу, не порождая никаких проблем
            //в принятом Error передаётся описание ошибки синхронизации профилей, если Error пуст - значит синхронизация была успешной
            if (this.FAfterSyncResultsRoutine != null)
                this.FAfterSyncResultsRoutine(error);
        }

        public void SyncResults(AfterSyncResultsRoutine afterSyncResultsRoutine)
        {
            //синхронизация результатов измерений выполняется продолжительное время, поэтому чтобы не получить ContextSwitchDeadlock выполняем её в отдельном потоке, при успешном завершении которого будет вызвана реализация afterSyncResultsRoutine
            LongTimeRoutineWorker Worker = new LongTimeRoutineWorker(SyncResultsWorkerHandler, SyncResultsCompletedHandler);

            //запоминаем что нам надо вызвать когда LongTimeRoutineWorker успешно выполнит свою работу
            this.FAfterSyncResultsRoutine = afterSyncResultsRoutine;

            //стартуем фоновый поток, нужды во входных параметрах нет - поэтому будем передавать null
            Worker.Run(null);
        }

        private void SyncProfilesWorkerHandler(DoWorkEventArgs workerParameters)
        {
            try
            {
                using (var centralDbClient = new CentralDatabaseServiceClient())
                {
                    var serverProfiles = centralDbClient.GetProfileItemsByMme(_mmeCode);
                    SaveProfiles(serverProfiles);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error while syncing profiles from local database with a master: {0}", ex.Message));
            }
        }

        private void SyncProfilesCompletedHandler(string Error)
        {
            //этот код выполняется в потоке диспетчера, поэтому как обычно обращаемся к разделяемым данным и пользовательскому интерфейсу, не порождая никаких проблем
            //в принятом Error передаётся описание ошибки синхронизации профилей, если Error пуст - значит синхронизация была успешной
            if (this.FAfterSyncProfilesRoutine != null)
                this.FAfterSyncProfilesRoutine(Error);
        }

        public void SyncProfiles(AfterSyncProfilesRoutine afterSyncProfilesRoutine)
        {
            //синхронизация профилей выполняется продолжительное время, поэтому чтобы не получить ContextSwitchDeadlock выполняем её в отдельном потоке
            LongTimeRoutineWorker Worker = new LongTimeRoutineWorker(SyncProfilesWorkerHandler, SyncProfilesCompletedHandler);

            //запоминаем что нам надо вызвать когда LongTimeRoutineWorker закончит свою работу
            this.FAfterSyncProfilesRoutine = afterSyncProfilesRoutine;

            //стартуем фоновый поток, нужды во входных параметрах нет - поэтому будем передавать null
            Worker.Run(null);
        }

        public void SyncProfiles(ICentralDatabaseService centralDatabaseService)
        {
            var serverProfiles = centralDatabaseService.GetProfileItemsByMme(_mmeCode);
            SaveProfiles(serverProfiles);
        }

        private void SaveProfiles(List<ProfileItem> serverProfiles)
        {
            _saveProfileService.SaveProfilesFromMme(serverProfiles, _mmeCode);
        }

        public void Dispose()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
                _connection.Close();
        }
    }
}
