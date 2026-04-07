define([
    "dojo/_base/declare",
    "dojo/topic",
    "dojo/when",
    "epi/shell/command/_Command",
    "epi-cms/_ContentContextMixin",
    "epi-cms/contentediting/ContentActionSupport",
    "epi/i18n!epi/cms/nls/reviewcomponent.command"
], function (
    declare,
    topic,
    when,
    _Command,
    _ContentContextMixin,
    ContentActionSupport,
    res
) {
    return declare([_Command, _ContentContextMixin], {
        name: "ContentReferences",
        label: res.label,
        tooltip: res.tooltip,
        iconClass: 'epi-icon--medium epi-review-icon',
        canExecute: false,

        constructor: function () {
            this._toggleCanExecute();

            topic.subscribe("reviews:force-review-mode", function () {
                when(this.getCurrentContent()).then(function (content) {
                    this._refreshContent(content);
                    if (this.canExecute) {
                        this.execute();
                    }
                }.bind(this));
            }.bind(this));

            topic.subscribe("reviews:disable", function () {
                this.set("active", false);
            }.bind(this));
        },

        contextChanged: function () {
            this.set("active", false);
            this._toggleCanExecute();
            topic.publish("reviews:toggle", this.active);
        },

        itemChanged: function (id, content) {
            this._refreshContent(content);
        },

        _refreshContent: function (content) {
            this.set({
                isAvailable: true,
                canExecute: content.status !== ContentActionSupport.versionStatus.Published
            });
        },

        _activeSetter: function (active) {
            this.active = active;
            this.set('label', active ? res.labelenabled : res.labelnotenabled);
        },

        _execute: function () {
            this.set("active", !this.active);
            topic.publish("reviews:toggle", this.active);
        },

        _toggleCanExecute: function () {
            return when(this.getCurrentContext()).then(function (context) {
                if (context.type === "epi.cms.contentdata") {
                    return when(this.getCurrentContent()).then(function (content) {
                        this._refreshContent(content);
                    }.bind(this));
                } else {
                    this.set({
                        isAvailable: false
                    });
                }

            }.bind(this));
        }
    });
});
