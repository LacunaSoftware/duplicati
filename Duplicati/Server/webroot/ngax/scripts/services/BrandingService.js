backupApp.service('BrandingService', function() {

    var state = { 
        'appName': 'Backup e-Notariado', 
        'appSubtitle': null,
        'appLogoPath': '../img/logo.png'
    };
    this.state = state;

    this.watch = function(scope, m) {
        scope.$on('brandingservicechanged', function() {
            if (m) m();

            $timeout(function() {
                scope.$digest();
            });
        });

        if (m) $timeout(m);
        return state;
    };
});
