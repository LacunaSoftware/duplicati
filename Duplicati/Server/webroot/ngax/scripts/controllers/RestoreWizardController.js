backupApp.controller('RestoreWizardController', function($scope, $location, BackupList, gettextCatalog) {
    $scope.backups = BackupList.watch($scope);

    $scope.selection = {
        backupid: '-1'
    };

    $scope.nextPage = function() {
        if ($scope.selection.backupid == 'direct')
            $location.path('/restoredirect');
        else if ($scope.selection.backupid == 'import')
            $location.path('/restore-import');
        else if ($scope.selection.backupid == 'enotariado')
            $location.path('/restore-enotariado');
        else
            $location.path('/restore/' + $scope.selection.backupid);
    };
});
