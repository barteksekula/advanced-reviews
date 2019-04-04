import React from "react";
import ReactDOM from "react-dom";
import { Provider } from "mobx-react";
import declare from "dojo/_base/declare";
import topic from "dojo/topic";
import WidgetBase from "dijit/_WidgetBase";
import _ContentContextMixin from "epi-cms/_ContentContextMixin";
import ReviewService from "alloy-review/advancedReviewService";
import ReviewLocationsCollection from "./reviewLocationsCollection";
import res from "epi/i18n!epi/cms/nls/reviewcomponent";

import { createStores } from "./reviewStore";
import IframeOverlay from "./iframeOverlay";

export default declare([WidgetBase, _ContentContextMixin], {
    postCreate: function() {
        //TODO: async
        this._reviewService = new ReviewService();
        this.own(this._reviewService);
        this.stores = createStores(this._reviewService, res);
        this.stores.reviewStore.load();

        ReactDOM.render(
            <Provider {...this.stores}>
                <IframeOverlay iframe={this.iframe}>
                    <ReviewLocationsCollection iframe={this.iframe} />
                </IframeOverlay>
            </Provider>,
            this.domNode
        );
    },
    contextChanged: function () {
        this.stores.reviewStore.load();
    },
    destroy: function() {
        ReactDOM.unmountComponentAtNode(this.domNode);
    }
});
