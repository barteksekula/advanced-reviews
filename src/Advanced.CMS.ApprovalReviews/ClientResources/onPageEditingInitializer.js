define([
    "dojo/topic",
    "dojo/dom-class",
    "epi-cms/contentediting/OnPageEditing",
    "advanced-cms-approval-reviews/dist/review-component-widget"
], function (
    topic,
    domClass,
    OnPageEditing,
    ReviewWidget) {

    var reviewWidget = null;

    var latestPropertyNameMappings = [];

    function toggleReviewOverlay(value) {
        if (reviewWidget) {
            domClass.toggle(reviewWidget.domNode, "dijitHidden", !value);
        }
    }

    function getDisplayNamesDictionary(editableNodes) {
        if (!editableNodes) {
            return {};
        }

        var result = {};
        editableNodes.filter(function (x) {
            return x.property && x.property.metadata;
        }).forEach(function (x) {
            // epi attributes are capital case but property names are lowercase
            var key = x.property.propertyNodeName[0].toUpperCase() + x.property.propertyNodeName.substring(1);
            result[key] = x.property.metadata.displayName;
            });
        return result;
    }

    function initialize(options, language, avatarUrl) {
        topic.subscribe("reviews:toggle", function (toggle) {
            if (!reviewWidget) {
                if (toggle) {
                    var div = document.createElement("div");
                    var visualBuilderContainer = document.getElementsByClassName("content-preview");
                    var visualBuilderMode = visualBuilderContainer.length === 1;

                    var iframes = document.getElementsByTagName("iframe");
                    var iframe = iframes.length > 1 ? iframes[1] : iframes[0];
                    reviewWidget = new ReviewWidget({
                        iframe: iframe,
                        language: language,
                        propertyNameMapping: latestPropertyNameMappings,
                        options: options,
                        avatarUrl: avatarUrl
                    });
                    reviewWidget.placeAt(div);
                    var editLayoutContainer = visualBuilderMode ? visualBuilderContainer[0] : document.getElementsByClassName("epi-editorViewport")[0];
                    editLayoutContainer.appendChild(div);
                }
            }
            else {
                reviewWidget.loadPins();
            }

            toggleReviewOverlay(toggle);
        });

        var originalOnReadySetupEditMode = OnPageEditing.prototype.onReadySetupEditMode;
        OnPageEditing.prototype.onReadySetupEditMode = function (doc) {
            originalOnReadySetupEditMode.apply(this, arguments);
            toggleReviewOverlay(false);
        };

        var originalCreateUpdateControllers = OnPageEditing.prototype._createUpdateControllers;
        OnPageEditing.prototype._createUpdateControllers = function (doc) {
            originalCreateUpdateControllers.apply(this, arguments);

            // create dictionary for names and display names
            var editableNodes = this._getEditableNodes(doc);
            latestPropertyNameMappings = getDisplayNamesDictionary(editableNodes);
            if (reviewWidget) {
                reviewWidget.updateDisplayNamesDictionary(latestPropertyNameMappings);
            }
        };

        var originalDestroy = OnPageEditing.prototype.destroy;
        OnPageEditing.prototype.destroy = function () {
            originalDestroy.apply(this, arguments);
            if (reviewWidget) {
                reviewWidget.destroy();
                reviewWidget = null;
            }
        };
    }

    return {
        initialize: initialize
    };
});
