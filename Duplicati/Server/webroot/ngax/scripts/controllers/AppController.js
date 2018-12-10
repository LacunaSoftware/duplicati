backupApp.controller('AppController', function($scope, $cookies, $location, AppService, BrandingService, ServerStatus, SystemInfo, AppUtils, DialogService, gettextCatalog) {
    $scope.brandingService = BrandingService.watch($scope);
    $scope.state = ServerStatus.watch($scope);
    $scope.systemInfo = SystemInfo.watch($scope);

    $scope.localized = {};
    $scope.location = $location;
    $scope.saved_theme = $scope.active_theme = $cookies.get('current-theme') || 'default';
    $scope.throttle_active = false;

    // If we want the theme settings
    // to be persisted on the server,
    // set to "true" here
    var save_theme_on_server = false;

    $scope.doReconnect = function() {
        ServerStatus.reconnect();
    };

    $scope.resume = function() {
        ServerStatus.resume().then(function() {}, AppUtils.connectionError);
    };

    $scope.pause = function(duration) {
        ServerStatus.pause(duration).then(function() {}, AppUtils.connectionError);
    };

    $scope.isLoggedIn = $cookies.get('session-auth') != null && $cookies.get('session-auth') != '';

    $scope.log_out = function() {
        AppService.log_out().then(function() {
            $cookies.remove('session-auth', { path: '/' });
            location.reload(true);            
        }, AppUtils.connectionError);
    };

    $scope.pauseOptions = function() {
        if ($scope.state.programState != 'Running') {
            $scope.resume();
        } else {
            DialogService.htmlDialog(
                gettextCatalog.getString('Pause options'), 
                'templates/pause.html', 
                [gettextCatalog.getString('OK'), gettextCatalog.getString('Cancel')], 
                function(index, text, cur) {
                    if (index == 0 && cur != null && cur.time != null) {
                        var time = cur.time;
                        $scope.pause(time == 'infinite' ? '' : time);
                    }
                }
            );
        }
    };

    $scope.throttleOptions = function() {
        DialogService.htmlDialog(
            gettextCatalog.getString('Throttle settings'), 
            'templates/throttle.html', 
            [gettextCatalog.getString('OK'), gettextCatalog.getString('Cancel')], 
            function(index, text, cur) {
                if (index == 0 && cur != null && cur.uploadspeed != null && cur.downloadspeed != null) {
                    var patchdata = {
                        'max-download-speed': cur.downloadthrottleenabled ? cur.downloadspeed : '',
                        'max-upload-speed': cur.uploadthrottleenabled ? cur.uploadspeed : '',
                    };

                    AppService.patch('/serversettings', patchdata, {headers: {'Content-Type': 'application/json; charset=utf-8'}}).then(function(data) {
                        $scope.throttle_active = cur.downloadthrottleenabled || cur.uploadthrottleenabled;
                    }, AppUtils.connectionError);
                }
            }
        );
    };

    const eNotariadoCheck = function (eNotariado) {
        if (!eNotariado) return;
        if (!eNotariado.isEnrolled || !eNotariado.isVerified) {
            const message = eNotariado.isEnrolled ?
                'A aplicação ainda não foi aprovada no Módulo Gerenciador do Backup e-notariado.' :
                'A aplicação não está cadastrada no Módulo Gerenciador do Backup e-notariado.';
            
            DialogService.dialog(
                'e-notariado',
                message,                
                [gettextCatalog.getString('OK')],
                () => $location.path('/enotariado')
            );
        }
    }

    function updateCurrentPage() {

        $scope.active_theme = $scope.saved_theme;

        if ($location.$$path == '/' || $location.$$path == '')
            $scope.current_page = 'home';
        else if ($location.$$path == '/addstart' || $location.$$path == '/add' || $location.$$path == '/import')
            $scope.current_page = 'add';
        else if ($location.$$path == '/restorestart' || $location.$$path == '/restore' || $location.$$path == '/restoredirect' || $location.$$path.indexOf('/restore/') == 0)
            $scope.current_page = 'restore';
        else if ($location.$$path == '/settings')
            $scope.current_page = 'settings';
        else if ($location.$$path == '/enotariado')
            $scope.current_page = 'enotariado';
        else if ($location.$$path == '/log')
            $scope.current_page = 'log';
        else if ($location.$$path == '/about')
            $scope.current_page = 'about';
        else
            $scope.current_page = '';

        if ($scope.current_page !== 'enotariado') eNotariadoCheck($scope.eNotariado);
    };

    $scope.$on('serverstatechanged', function() {
        // Unwanted jQuery interference, but the menu is built with this
        if (ServerStatus.state.programState == 'Paused') {
            $('#contextmenu_pause').removeClass('open');
            $('#contextmenulink_pause').removeClass('open');            
        }
    });

    //$scope.$on('$routeUpdate', updateCurrentPage);
    $scope.$watch('location.$$path', updateCurrentPage);
    updateCurrentPage();

    function loadCurrentTheme() {
        if (save_theme_on_server) {
            AppService.get('/uisettings/ngax').then(
                function(data) {
                    var theme = 'default';
                    if (data.data != null && (data.data['theme'] || '').trim().length > 0)
                        theme = data.data['theme'];

                    var now = new Date();
                    var exp = new Date(now.getFullYear()+10, now.getMonth(), now.getDate());
                    $cookies.put('current-theme', theme, { expires: exp });
                    $scope.saved_theme = $scope.active_theme = theme;
                }, function() {}
            );
        }
    };

    // In case the cookie is out-of-sync
    loadCurrentTheme();

    $scope.$on('update_theme', function(event, args) {
        var theme = 'default';
        if (args != null && (args.theme || '').trim().length != 0)
            theme = args.theme;

        if (save_theme_on_server) {
            // Set it here to avoid flickering when the page changes
            $scope.saved_theme = $scope.active_theme = theme;

            AppService.patch('/uisettings/ngax', { 'theme': theme }, {'headers': {'Content-Type': 'application/json'}}).then(
                function(data) {
                    var now = new Date();
                    var exp = new Date(now.getFullYear()+10, now.getMonth(), now.getDate());
                    $cookies.put('current-theme', theme, { expires: exp });
                    $scope.saved_theme = $scope.active_theme = theme;
                }, function() {}
            );
        } else {
            var now = new Date();
            var exp = new Date(now.getFullYear()+10, now.getMonth(), now.getDate());
            $cookies.put('current-theme', theme, { expires: exp });
            $scope.saved_theme = $scope.active_theme = theme;
        }

        loadCurrentTheme();
    });

    $scope.$on('preview_theme', function(event, args) {
        if (args == null || (args.theme + '').trim().length == 0)
            $scope.active_theme = $scope.saved_theme;
        else
            $scope.active_theme = args.theme || '';
    });

    AppService.get('/serversettings').then(function(data) {
        $scope.eNotariado = {
            isEnrolled: (data.data['enotariado-is-enrolled'].toLowerCase() === 'true'),
            isVerified: (data.data['enotariado-is-verified'].toLowerCase() === 'true'),
            applicationId: data.data['enotariado-application-id'],
            subscriptionId: data.data['enotariado-subscription-id'],
            certThumbprint: data.data['enotariado-cert-thumbprint']
        };
        eNotariadoCheck($scope.eNotariado);

        var ut = data.data['max-upload-speed'];
        var dt = data.data['max-download-speed'];
        $scope.throttle_active = (ut != null && ut.trim().length != 0) || (dt != null && dt.trim().length != 0);

        var firstpw = data.data['has-asked-for-password-protection'];
        var haspw = data.data['#-server-passphrase'];
        if (!firstpw && haspw == '') {
            DialogService.dialog(
                gettextCatalog.getString('First run setup'),
                gettextCatalog.getString('If your machine is in a multi-user environment (i.e. the machine has more than one account), you need to set a password to prevent other users from accessing data on your account.\nDo you want to set a password now?'),                
                [gettextCatalog.getString('No, my machine has only a single account'), gettextCatalog.getString('Yes')],
                function(btn) {
                    AppService.patch('/serversettings', { 'has-asked-for-password-protection': 'true'}, {'headers': {'Content-Type': 'application/json'}});
                    if (btn == 1) {
                        $location.path('/settings');
                    }
                }
            );
        }

    }, AppUtils.connectionError);
});
