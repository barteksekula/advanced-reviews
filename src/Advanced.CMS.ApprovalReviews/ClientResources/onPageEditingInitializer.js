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

    function getVisualBuilderContainer() {
        var containers = document.getElementsByClassName("content-preview");
        return containers.length === 1 ? containers[0] : null;
    }

    function getPreviewIframe() {
        var visualBuilderContainer = getVisualBuilderContainer();
        var previewIframe = visualBuilderContainer && visualBuilderContainer.querySelector("iframe");
        if (previewIframe) {
            return previewIframe;
        }

        // take the last iframe, we know it has to be from the preview
        var iframes = document.getElementsByTagName("iframe");
        return iframes.length > 1 ? iframes[iframes.length - 1] : iframes[0];
    }

    function isDetached(widget) {
        return !widget.domNode || !widget.domNode.isConnected || !widget.iframe || !widget.iframe.isConnected ||
            widget.iframe !== getPreviewIframe();
    }

    function destroyReviewWidget() {
        if (!reviewWidget) {
            return;
        }

        var container = reviewWidget.domNode && reviewWidget.domNode.parentNode;
        reviewWidget.destroy();
        reviewWidget = null;
        if (container && container.parentNode) {
            container.parentNode.removeChild(container);
        }
    }

    function initialize(options, language, avatarUrl) {
        topic.subscribe("/epi/shell/action/changeview", function () {
            reviewWidget = null;
            topic.publish("reviews:disable", false);
        });

        topic.subscribe("reviews:toggle", function (toggle) {
            if (reviewWidget && isDetached(reviewWidget)) {
                destroyReviewWidget();
            }

            if (!reviewWidget) {
                if (toggle) {
                    var div = document.createElement("div");
                    var visualBuilderContainer = getVisualBuilderContainer();
                    var visualBuilderMode = !!visualBuilderContainer;
                    var iframe = getPreviewIframe();
                    reviewWidget = new ReviewWidget({
                        iframe: iframe,
                        language: language,
                        propertyNameMapping: latestPropertyNameMappings,
                        options: options,
                        avatarUrl: avatarUrl
                    });
                    reviewWidget.placeAt(div);
                    var editLayoutContainer = visualBuilderMode ? visualBuilderContainer : document.getElementsByClassName("epi-editorViewport")[0];
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
            destroyReviewWidget();
        };
    }

    return {
        initialize: initialize
    };
});
