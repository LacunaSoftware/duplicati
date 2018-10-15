backupApp.controller('RestoreDirectController', function ($rootScope, $scope, $location, AppService, AppUtils, SystemInfo, ServerStatus, DialogService, gettextCatalog) {

    $scope.SystemInfo = SystemInfo.watch($scope);
    $scope.AppUtils = AppUtils;
    $scope.ServerStatus = ServerStatus;
    $scope.serverstate = ServerStatus.watch($scope);
    $scope.EncryptionPassphrase = $scope.BackupENotariadoPassword;

    var dlg;

    function handleError(data) {
        if (dlg != null)
            dlg.dismiss();
        
        AppUtils.connectionError(data);
    }
    
    function getPassphrase() {        
        if ($scope.EncryptionPassphrase === undefined) {
            dlg = DialogService.dialog(gettextCatalog.getString('Security'), gettextCatalog.getString('Retrieving password to restore backup...'), [], null, function() {       
                AppService.get('/enotariado/backup-password').then(
                    function(resp) {
                        dlg.dismiss();
                        $scope.EncryptionPassphrase = resp.data.Password;
                    }, handleError
                );
            });
        }
    }

    $scope.CurrentStep = 0;
    $scope.connecting = false;
    $scope.logs = [];
    $scope.backups = [];

    $scope.nextPage = function() {
        $scope.CurrentStep = Math.min(1, $scope.CurrentStep + 1);
    };

    $scope.prevPage = function() {
        $scope.CurrentStep = Math.max(0, $scope.CurrentStep - 1);
    };

    $scope.doConnect = function() {
        $scope.CurrentStep = 1;
        $scope.connecting = true;
        $scope.logs.push(gettextCatalog.getString('Getting list of backups stored remotely ...'));

        AppService.get('/enotariado/backup-list', {'headers': {'Content-Type': 'application/json'}}).then(
            function(resp) {
                
                $scope.backups = resp.data;
                $scope.logs.push(AppUtils.format(gettextCatalog.getString('Retrieved information about {0} backups ...'), resp.data.length));
            }, function(resp) {
                $scope.connecting = false;
                AppUtils.connectionError(resp);
            }
        );
    };

    $scope.restore = function() {
        var backupInfo = JSON.parse($scope.selectedBackup);
        var targetURL = `enotariado://${backupInfo.ContainerName}?name=${backupInfo.BackupName}`;
        var opts = {};
        var obj = {'Backup': {'TargetURL': targetURL } };

        if (($scope.EncryptionPassphrase || '') == '')
            opts['--no-encryption'] = 'true';
        else
            opts['passphrase'] = $scope.EncryptionPassphrase;

        if (!AppUtils.parse_extra_options($scope.ExtendedOptions, opts))
            return false;

        obj.Backup.Settings = [];
        for(var k in opts) {
            obj.Backup.Settings.push({
                Name: k,
                Value: opts[k]
            });
        }

        AppService.post('/backups?temporary=true', obj, {'headers': {'Content-Type': 'application/json'}}).then(
            function(resp) {

                $scope.logs.push(gettextCatalog.getString('Listing backup dates ...'));
                $scope.BackupID = resp.data.ID;
                $scope.fetchBackupTimes();
            }, function(resp) {
                $scope.connecting = false;
                AppUtils.connectionError(resp);
            }
        );
    }
    
    $scope.fetchBackupTimes = function() {
        AppService.get('/backup/' + $scope.BackupID + '/filesets').then(
            function(resp) {
                // Pass the filesets through a global variable
                if ($rootScope.filesets == null)
                    $rootScope.filesets = {};
                $rootScope.filesets[$scope.BackupID] = resp.data;
                $location.path('/restore/' + $scope.BackupID);
            },

            function(resp) {
                var message = resp.statusText;
                if (resp.data != null && resp.data.Message != null)
                    message = resp.data.Message;
                else if (resp.data != null && resp.data.Error != null)
                    message = resp.data.Error;

                if (message == 'encrypted-storage')
                    message = gettextCatalog.getString('The target folder contains encrypted files, please supply the passphrase');

                $scope.connecting = false;
                DialogService.dialog(gettextCatalog.getString('Error'), gettextCatalog.getString('Failed to connect: {{message}}', { message: message }));
            }
        );
    };

    getPassphrase();
});
