backupApp.controller('RestoreENotariadoController', function ($rootScope, $scope, $location, AppService, AppUtils, SystemInfo, ServerStatus, DialogService, gettextCatalog) {
    $scope.SystemInfo = SystemInfo.watch($scope);
    $scope.AppUtils = AppUtils;
    $scope.ServerStatus = ServerStatus;
    $scope.serverstate = ServerStatus.watch($scope);

    $scope.CurrentStep = 0;
    $scope.connecting = false;
    $scope.ConnectionProgress = [];
    let backups = [];

    $scope.nextPage = function() {
        $scope.CurrentStep = Math.min(1, $scope.CurrentStep + 1);
    };

    $scope.prevPage = function() {
        $scope.CurrentStep = Math.max(0, $scope.CurrentStep - 1);
    };
    $scope.doConnect = function() {
        $scope.CurrentStep = 1;
        $scope.connecting = true;
        $scope.ConnectionProgress.push(gettextCatalog.getString('Getting list of backups stored remotely ...'));

        AppService.get('/enotariado', {'headers': {'Content-Type': 'application/json'}}).then(
            function(resp) {
                
                backups = resp.data;
                $scope.ConnectionProgress.push(AppUtils.format(gettextCatalog.getString('Retrieved information about {0} backups ...'), resp.data.length));
            }, function(resp) {
                var message = resp.statusText;
                if (resp.data != null && resp.data.Message != null)
                    message = resp.data.Message;

                $scope.connecting = false;
                $scope.ConnectionProgress.push(gettextCatalog.getString('Failed to connect: {{message}}', { message: message }));
            }
        );
    };

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

                if (message == 'encrypted-storage')
                    message = gettextCatalog.getString('The target folder contains encrypted files, please supply the passphrase');

                $scope.connecting = false;
                $scope.ConnectionProgress = '';
                DialogService.dialog(gettextCatalog.getString('Error'), gettextCatalog.getString('Failed to connect: {{message}}', { message: message }));
            }
        );
    };

});
