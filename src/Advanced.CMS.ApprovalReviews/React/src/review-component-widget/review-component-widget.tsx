import ReviewService from "advanced-cms-approval-reviews/advancedReviewService";
import WidgetBase from "dijit/_WidgetBase";
import declare from "dojo/_base/declare";
import res from "epi/i18n!epi/cms/nls/reviewcomponent";
import ApplicationSettings from "epi-cms/ApplicationSettings";
import { Provider } from "mobx-react";
import React from "react";
import { createRoot } from "react-dom/client";

import IframeWithPins from "../iframe-with-pins/iframe-with-pins";
import { createStores } from "../store/review-store";

export default declare([WidgetBase], {
    postCreate: function () {
        //TODO: async
        this._reviewService = new ReviewService();
        this.own(this._reviewService);
        this.stores = createStores(this._reviewService, res);
        this.loadPins();
        this.stores.reviewStore.currentUser = ApplicationSettings.userName;
        this.stores.reviewStore.currentLocale = this.language;
        this.stores.reviewStore.propertyNameMapping = this.propertyNameMapping;
        this.stores.reviewStore.options = this.options;
        this.stores.reviewStore.avatarUrl = this.avatarUrl;

        this.root = createRoot(this.domNode);
        this.root.render(
            <Provider {...this.stores}>
                <IframeWithPins iframe={this.iframe} />
            </Provider>,
        );
    },

    /**
     *
     * @param propertyNameMapping dictionary with name and displayname pairs
     */
    updateDisplayNamesDictionary: function (propertyNameMapping: object) {
        this.stores.reviewStore.updateDisplayNamesDictionary(propertyNameMapping);
    },
    loadPins: function () {
        this.stores.reviewStore.load();
    },
    destroy: function () {
        this.root.unmount();
    },
});
