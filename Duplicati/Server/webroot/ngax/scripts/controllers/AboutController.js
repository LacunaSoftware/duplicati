backupApp.controller('AboutController', function($scope, $location, BrandingService, ServerStatus, AppService, SystemInfo, AppUtils, gettextCatalog, DialogService) {
    $scope.brandingService = BrandingService.watch($scope);
    $scope.Page = 'general';
    $scope.sysinfo = SystemInfo.watch($scope);
    $scope.state = ServerStatus.watch($scope);

    // Common licenses
    var licenses = {
        'MIT': 'http://www.linfo.org/mitlicense.html',
        'Apache': 'https://www.apache.org/licenses/LICENSE-2.0.html',
        'Apache 2': 'https://www.apache.org/licenses/LICENSE-2.0.html',
        'Apache 2.0': 'https://www.apache.org/licenses/LICENSE-2.0.html',
        'Public Domain': 'https://creativecommons.org/licenses/publicdomain/',
        'GPL': 'https://www.gnu.org/copyleft/gpl.html',
        'LGPL': 'https://www.gnu.org/copyleft/lgpl.html',
        'MS-PL': 'http://opensource.org/licenses/MS-PL',
        'Microsoft Public': 'http://opensource.org/licenses/MS-PL',
        'New BSD': 'http://opensource.org/licenses/BSD-3-Clause'
    };

    AppService.get('/acknowledgements').then(function(resp) {
        $scope.Acknowledgements = resp.data.Acknowledgements;
    });

    $scope.$watch('Page', function() {
        if ($scope.Page == 'changelog' && $scope.ChangeLog == null) {
            AppService.get('/changelog?from-update=false').then(function(resp) {
                $scope.ChangeLog =     resp.data.Changelog;
            });
        } else if ($scope.Page == 'licenses' && $scope.Licenses == null) {
            AppService.get('/licenses').then(function(resp) {
                var res = [];
                for(var n in resp.data) {
                    var r = JSON.parse(resp.data[n].Jsondata);
                    if (r != null) {
                        r.licenselink = r.licenselink || licenses[r.license] || '#';
                        res.push(r);
                    }                    
                }
                $scope.Licenses = res;
            });
        }
    });

    $scope.doShowUpdateChangelog = function() {
        $location.path('/updatechangelog');
    };

    $scope.doStartUpdateDownload = function() {
        AppService.post('/updates/install');
    };

    $scope.doStartUpdateActivate = function() {
        if ($scope.state.activeTask == null) {
            const oldVersion = $scope.sysinfo.ServerVersion;
            DialogService.dialog(gettextCatalog.getString('Warning'), gettextCatalog.getString('Ao continuar o Módulo Agente será reiniciado e quaisquer operações ocorrendo atualmente serão perdidas, continuar?'), [gettextCatalog.getString('No'), gettextCatalog.getString('Yes')], function(ix) {
                if (ix == 1) {
                    AppService.post('/updates/activate').then(function() {
                        var interval = setInterval(() => {
                            SystemInfo.loadSystemInfo(true, () => {});
                            if (oldVersion != $scope.sysinfo.ServerVersion) {
                                console.log(oldVersion + ' ayy ' + $scope.sysinfo.ServerVersion);
                                window.location.reload();
                                clearInterval(interval);
                            }
                        }, 1000);
                    }, AppUtils.connectionError("Falha ao ativar a atualização: "));
                }
            });
        }
        else {
            DialogService.dialog(gettextCatalog.getString('Error'), gettextCatalog.getString('A nova atualização não pode ser ativada enquanto houverem tarefas sendo executadas'));
        }
    };

    $scope.doCheckForUpdates = function() {
        AppService.post('/updates/check').then(() => {}, 
            AppUtils.connectionError("Falha ao conferir atualizações: "));

    };

});
