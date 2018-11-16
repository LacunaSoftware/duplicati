backupApp.controller('SystemSettingsController', function($rootScope, $scope, $location, $cookies, AppService, AppUtils, SystemInfo, DialogService, gettextCatalog) {

    let dlg;
    $scope.SystemInfo = SystemInfo.watch($scope);    
    $scope.theme = $scope.$parent.$parent.saved_theme;
    $scope.enrollSettings = false;
    if (($scope.theme || '').trim().length == 0)
        $scope.theme = 'default';

    $scope.usageReporterLevel = '';

    function reloadOptionsList() {
        $scope.advancedOptionList = AppUtils.buildOptionList($scope.SystemInfo, false, false, false);
        var mods = [];
        if ($scope.SystemInfo.ServerModules != null)
            for(var ix in $scope.SystemInfo.ServerModules)
            {
                var m = $scope.SystemInfo.ServerModules[ix];
                if (m.SupportedGlobalCommands != null && m.SupportedGlobalCommands.length > 0)
                    mods.push(m);
            }

        $scope.ServerModules = mods;
        AppUtils.extractServerModuleOptions($scope.advancedOptions, $scope.ServerModules, $scope.servermodulesettings, 'SupportedGlobalCommands');
    };

    reloadOptionsList();
    $scope.$on('systeminfochanged', reloadOptionsList);

    $scope.$watch('theme', function() {
        $rootScope.$broadcast('preview_theme', { theme: $scope.theme });
    });

    $scope.uiLanguage = $cookies.get('ui-locale');
    $scope.lang_browser_default = gettextCatalog.getString('Browser default');
    $scope.lang_default = gettextCatalog.getString('Default');

    function setUILanguage() {
        if (($scope.uiLanguage || '').trim().length == 0) {
            $cookies.remove('ui-locale');
            gettextCatalog.setCurrentLanguage($scope.SystemInfo.BrowserLocale.Code.replace("-", "_"));
        } else {
            var now = new Date();
            var exp = new Date(now.getFullYear()+10, now.getMonth(), now.getDate());
            $cookies.put('ui-locale', $scope.uiLanguage, { expires: exp });

            gettextCatalog.setCurrentLanguage($scope.uiLanguage.replace("-", "_"));
        }
        $rootScope.$broadcast('ui_language_changed');
    };

    function handleError(data) {
        if (dlg != null)
            dlg.dismiss();
        
        AppUtils.connectionError(data);
        getSettings();
    }

    function getSettings() {
        AppService.get('/serversettings').then(function(data) {
            data.data['placeholder-password'] = AppUtils.parseBoolString(data.data['has-password-protection']) ? Math.random().toString(36) : '';
            $scope.rawdata = data.data;        

            $scope.requireRemotePassword = AppUtils.parseBoolString(data.data['has-password-protection']);
            $scope.remotePassword = data.data['placeholder-password'];
            $scope.confirmPassword = '';
            $scope.allowRemoteAccess = data.data['server-listen-interface'] != 'loopback';
            $scope.startupDelayDurationValue = data.data['startup-delay'].substr(0, data.data['startup-delay'].length - 1) == "" ? "0" : data.data['startup-delay'].substr(0, data.data['startup-delay'].length - 1);
            $scope.startupDelayDurationMultiplier = data.data['startup-delay'].substr(-1) == "" ? "s" : data.data['startup-delay'].substr(-1);
            $scope.updateChannel = data.data['update-channel'];
            $scope.originalUpdateChannel = data.data['update-channel'];
            $scope.usageReporterLevel = data.data['usage-reporter-level'];
            $scope.disableTrayIconLogin =  AppUtils.parseBoolString(data.data['disable-tray-icon-login']);
            $scope.remoteHostnames = data.data['allowed-hostnames'];
            $scope.advancedOptions = AppUtils.serializeAdvancedOptionsToArray(data.data);
            $scope.servermodulesettings = {};
            if (!$scope.eNotariado) $scope.eNotariado = {};
            $scope.eNotariado.isEnrolled = (data.data['enotariado-is-enrolled'].toLowerCase() === 'true');
            $scope.eNotariado.isVerified = (data.data['enotariado-is-verified'].toLowerCase() === 'true');
            $scope.eNotariado.applicationId = data.data['enotariado-application-id'];
            $scope.eNotariado.certThumbprint = data.data['enotariado-cert-thumbprint'];

            AppUtils.extractServerModuleOptions($scope.advancedOptions, $scope.ServerModules, $scope.servermodulesettings, 'SupportedGlobalCommands');
            
        }, AppUtils.connectionError);
    }
    getSettings();

    $scope.eNotariadoReset = function() {
        dlg = DialogService.dialog('Conectando...', 'Redefinindo dados de cadastro com o e-notariado ...', [], null, function() {       
            AppService.post('/enotariado/reset').then(
                function() {
                    dlg.dismiss();
                    dlg = DialogService.dialog('Sucesso', 'Dados redefinidos, a aplicação está pronta para ser reiniciada!');
                    dlg.ondismiss = getSettings
                }, (resp) => {
                    if (dlg != null) dlg.dismiss();
                    AppUtils.connectionError(resp, undefined, 'Falha na conexão');
                    getSettings();
                }
            );
        });
    }

    $scope.copyToClipboard = function() {
        const textArea = document.createElement("textarea");
        const sliced = $scope.eNotariado.applicationId;
        textArea.value = sliced;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        try {
            const success = document.body.removeChild(textArea);
            if (success) {
                DialogService.dialog(gettextCatalog.getString('Success'), AppUtils.format(gettextCatalog.getString('Copied {0} to clipboard'), sliced));
            } else {
                throw Exception;
            }
        } catch (err) {
            DialogService.dialog(gettextCatalog.getString('Fail'), AppUtils.format(gettextCatalog.getString('Could not copy {0} to clipboard'), sliced));

        }
    }

    $scope.save = function() {

        if ($scope.requireRemotePassword && $scope.remotePassword.trim().length == 0)
            return AppUtils.notifyInputError('Cannot use empty password');

        var patchdata = {
            'allowed-hostnames': $scope.remoteHostnames,
            'server-listen-interface': $scope.allowRemoteAccess ? 'any' : 'loopback',
            'startup-delay': $scope.startupDelayDurationValue + '' + $scope.startupDelayDurationMultiplier,
            'update-channel': $scope.updateChannel,
            'usage-reporter-level': $scope.usageReporterLevel,
            'disable-tray-icon-login': $scope.disableTrayIconLogin
        };

        if ($scope.requireRemotePassword && ($scope.remotePassword != $scope.rawdata['placeholder-password'])) {
            if ($scope.remotePassword != $scope.confirmPassword) {
                AppUtils.notifyInputError(gettextCatalog.getString('The passwords do not match'));
                return;
            }
            patchdata['#-server-passphrase-salt'] =  CryptoJS.lib.WordArray.random(256/8).toString(CryptoJS.enc.Base64);
            patchdata['#-server-passphrase'] = CryptoJS.SHA256(CryptoJS.enc.Hex.parse(CryptoJS.enc.Utf8.parse($scope.remotePassword) + CryptoJS.enc.Base64.parse(patchdata['#-server-passphrase-salt']))).toString(CryptoJS.enc.Base64);
        } else if (!$scope.requireRemotePassword) {
            patchdata['#-server-passphrase-salt'] = null;
            patchdata['#-server-passphrase'] = null;
        }

        AppUtils.mergeAdvancedOptions($scope.advancedOptions, patchdata, $scope.rawdata);
        for(var n in $scope.servermodulesettings)
            patchdata['--' + n] = $scope.servermodulesettings[n];

        $rootScope.$broadcast('update_theme', { theme: $scope.theme } );

        AppService.patch('/serversettings', patchdata, {headers: {'Content-Type': 'application/json; charset=utf-8'}}).then(
            function() {
                setUILanguage();

                // Check for updates if we changed the channel
                if ($scope.updateChannel != $scope.originalUpdateChannel)
                    AppService.post('/updates/check');

                $location.path('/');
            },
            AppUtils.connectionError(gettextCatalog.getString('Failed to save:') + ' ')
        );
    };
});
