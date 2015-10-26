(function () {

    var reloadHub = $.connection.reloadHub;
    $.connection.hub.logging = true;
    $.connection.hub.start();

    reloadHub.client.sendMessage = function (str) {
        model.addMessage(str);
    };

    reloadHub.client.newApps = function (apps) {
        model.appIdentifiers(apps);
    };

    reloadHub.client.newConnections = function (cons) {
        model.connections(cons);
    };

    var Model = function () {
        var self = this;
        self.script = ko.observable(""),
        self.messages = ko.observableArray(),
        self.selectedApp = ko.observable(),
        self.selectedCon = ko.observable(),
        self.appIdentifiers = ko.observableArray(),
        self.connections = ko.observableArray()
    };

    Model.prototype = {
        reloadScript: function () {
            var self = this;
            reloadHub.server.reload(self.selectedCon(), self.selectedApp(), self.script());
        },

        connect: function () {
            var self = this;
            reloadHub.server.setActiveServer(self.selectedCon());
        },

        addMessage: function (str) {
            var self = this;
            self.messages.push(str);
        },

        connections: function (cons) {
            var self = this;
            self.connections.push(cons);
        },

        appIdentifiers: function (appIdentifiers) {
            var self = this;
            self.appIdentifiers.push(appIdentifiers);
        }
    };

    var model = new Model();

    $(function () {
        ko.applyBindings(model);
    });

}());