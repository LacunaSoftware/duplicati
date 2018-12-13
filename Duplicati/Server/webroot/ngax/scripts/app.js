var backupApp = angular.module(
    'backupApp', 
    [
        'ngRoute', 
        'dotjem.angular.tree',
        'ngCookies',
        'ngSanitize',
        'gettext',
        'ngclipboard'
    ]
);

backupApp.constant('appConfig', {
    login_url: '/login.html?v=1.0.0'
});

backupApp.config(['$routeProvider',
    function($routeProvider) {
        $routeProvider.
            when('/home', {
                templateUrl: 'templates/home.html?v=1.0.0'
            }).
            when('/add', {
                templateUrl: 'templates/addoredit.html?v=1.0.0'
            }).
            when('/add-import', {
                templateUrl: 'templates/addoredit.html?v=1.0.0'
            }).
            when('/restorestart', {
                templateUrl: 'templates/restorewizard.html?v=1.0.0'
            }).
            when('/addstart', {
                templateUrl: 'templates/addwizard.html?v=1.0.0'
            }).
            when('/edit/:backupid', {
                templateUrl: 'templates/addoredit.html?v=1.0.0'
            }).
            when('/restoredirect', {
                templateUrl: 'templates/restoredirect.html?v=1.0.0'
            }).
            when('/restoredirect-import', {
                templateUrl: 'templates/restoredirect.html?v=1.0.0'
            }).
            when('/restore/:backupid', {
                templateUrl: 'templates/restore.html?v=1.0.0'
            }).
            when('/settings', {
                templateUrl: 'templates/settings.html?v=1.0.0'
            }).
            when('/enotariado', {
                templateUrl: 'templates/enotariado.html?v=1.0.0'
            }).
            when('/about', {
                templateUrl: 'templates/about.html?v=1.0.0'
            }).
            when('/delete/:backupid', {
                templateUrl: 'templates/delete.html?v=1.0.0'
            }).
            when('/log/:backupid', {
                templateUrl: 'templates/backuplog.html?v=1.0.0'
            }).
            when('/log', {
                templateUrl: 'templates/log.html?v=1.0.0'
            }).
            when('/updatechangelog', {
                templateUrl: 'templates/updatechangelog.html?v=1.0.0'
            }).
            when('/export/:backupid', {
                templateUrl: 'templates/export.html?v=1.0.0'
            }).
            when('/import', {
                templateUrl: 'templates/import.html?v=1.0.0'
            }).
            when('/restore-import', {
                templateUrl: 'templates/import.html?v=1.0.0'
            }).
            when('/restore-enotariado', {
                templateUrl: 'templates/restoredirect.html?v=1.0.0'
            }).
            when('/localdb/:backupid', {
                templateUrl: 'templates/localdatabase.html?v=1.0.0'
            }).
            when('/commandline', {
                templateUrl: 'templates/commandline.html?v=1.0.0'
            }).
            when('/commandline/:backupid', {
                templateUrl: 'templates/commandline.html?v=1.0.0'
            }).
            when('/commandline/view/:viewid', {
                templateUrl: 'templates/commandline.html?v=1.0.0'
            }).
            otherwise({
                templateUrl: 'templates/home.html?v=1.0.0'
                //redirectTo: '/home'
        });
}]);

backupApp.run(function($injector) {
    try {
        $injector.get('OEMService');
    } catch(e) {}
    try {
        $injector.get('CustomService');
    } catch(e) {}
    try {
        $injector.get('ProxyService');
    } catch(e) {}
});

// Registers a global parseInt function
angular.module('backupApp').run(function($rootScope){
    $rootScope.parseInt = function(str) {
        return parseInt(str);
    };  
});

// Register a global back function
/*backupApp.run(function ($rootScope, $location) {

    var history = [];
    $rootScope.$on('$routeChangeSuccess', function() {
        history.push($location.$$path);
    });

    $rootScope.back = function () {
        var prevUrl = history.length > 1 ? history.splice(-2)[0] : "/home";
        $location.path(prevUrl);
    };

});*/
