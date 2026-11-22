define([
    "dojo/_base/declare",
    "dojo/topic",
    "dojo/dom-class",
    "dojo/when",
    "epi/dependency",
    "epi-cms/_ContentContextMixin",
    "epi-cms/contentediting/OnPageEditing",
    "advanced-cms-approval-reviews/dist/review-component-widget"
], function (
    declare,
    topic,
    domClass,
    when,
    dependency,
    _ContentContextMixin,
    OnPageEditing,
    ReviewWidget) {

    var reviewWidget = null;

    var latestPropertyNameMappings = {};

    var metadataDisplayNames = {};

    var contentContext = null;

    function getPropertyNameMapping() {
        return Object.assign({}, metadataDisplayNames, latestPropertyNameMappings);
    }

    function loadMetadataDisplayNames() {
        contentContext = contentContext || new (declare([_ContentContextMixin], {}))();
        return when(contentContext.getCurrentContext()).then(function (context) {
            if (!context || context.type !== "epi.cms.contentdata") {
                return null;
            }
            return dependency.resolve("epi.shell.MetadataManager")
                .getMetadataForType("EPiServer.Core.ContentData", { contentLink: context.id });
        }).then(function (metadata) {
            var result = {};
            addMetadataDisplayNames(result, metadata && metadata.properties);
            metadataDisplayNames = result;
            if (reviewWidget) {
                reviewWidget.updateDisplayNamesDictionary(getPropertyNameMapping());
            }
        }, function (error) {
            console.warn("Couldn't load property display names", error);
        });
    }

    function toggleReviewOverlay(value) {
        if (reviewWidget) {
            domClass.toggle(reviewWidget.domNode, "dijitHidden", !value);
        }
    }

    function toPascalCase(name) {
        return name[0].toUpperCase() + name.substring(1);
    }

    var legacyPropertyNames = {
        icontent_name: "PageName",
        iroutable_routesegment: "PageURLSegment"
    };

    function addMetadataDisplayNames(result, properties) {
        (properties || []).forEach(function (property) {
            if (property.name && property.displayName) {
                result[toPascalCase(property.name)] = property.displayName;
                var legacyName = legacyPropertyNames[property.name.toLowerCase()];
                if (legacyName) {
                    result[legacyName] = property.displayName;
                }
            }
            addMetadataDisplayNames(result, property.properties);
        });
    }

    function getDisplayNamesDictionary(editableNodes) {
        var result = {};
        if (!editableNodes) {
            return result;
        }

        editableNodes.filter(function (x) {
            return x.property && x.property.metadata;
        }).forEach(function (x) {
            // epi attributes are capital case but property names are lowercase
            result[toPascalCase(x.property.propertyNodeName)] = x.property.metadata.displayName;
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
                        propertyNameMapping: getPropertyNameMapping(),
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

            if (toggle) {
                loadMetadataDisplayNames();
            }
        });

        var originalOnReadySetupEditMode = OnPageEditing.prototype.onReadySetupEditMode;
        OnPageEditing.prototype.onReadySetupEditMode = function (doc) {
            originalOnReadySetupEditMode.apply(this, arguments);
            toggleReviewOverlay(false);
        };

        var originalCreateUpdateControllers = OnPageEditing.prototype._createUpdateControllers;
        OnPageEditing.prototype._createUpdateControllers = function (doc, props) {
            originalCreateUpdateControllers.apply(this, arguments);

            // create dictionary for names and display names
            var editableNodes = doc || !this._getEditableNodesFromEpiProps
                ? this._getEditableNodes(doc)
                : this._getEditableNodesFromEpiProps(props);
            latestPropertyNameMappings = getDisplayNamesDictionary(editableNodes);
            if (reviewWidget) {
                reviewWidget.updateDisplayNamesDictionary(getPropertyNameMapping());
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
