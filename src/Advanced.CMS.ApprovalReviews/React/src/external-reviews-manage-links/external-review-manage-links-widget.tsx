import ExternalReviewService from "advanced-cms-external-reviews/external-review-service";
import WidgetBase from "dijit/_WidgetBase";
import declare from "dojo/_base/declare";
import res from "epi/i18n!epi/cms/nls/externalreviews";
import _ContentContextMixin from "epi-cms/_ContentContextMixin";
import React from "react";
import { createRoot } from "react-dom/client";

import { ExternalReviewStore } from "./external-review-links-store";
import ManageLinks from "./external-review-manage-links";

/**
 * Edit Mode component used to list external links
 */
export default declare([WidgetBase, _ContentContextMixin], {
    postCreate: function () {
        if (!this.params.isEnabled) {
            return;
        }

        this._reviewService = new ExternalReviewService();

        this.own(this._reviewService);

        this.store = new ExternalReviewStore(this._reviewService);
        this.own(this.store);
        this.store.load();
        this.store.initialMailSubject = this.params.initialMailSubject;
        this.store.initialViewMailMessage = this.params.initialViewMailMessage;
        this.store.initialEditMailMessage = this.params.initialEditMailMessage;

        this.root = createRoot(this.domNode);

        this.root.render(
            <ManageLinks
                store={this.store}
                editableLinksEnabled={this.params.editableLinksEnabled}
                pinCodeSecurityEnabled={this.params.pinCodeSecurityEnabled}
                prolongDays={this.params.prolongDays}
                pinCodeSecurityRequired={this.params.pinCodeSecurityRequired}
                pinCodeLength={this.params.pinCodeLength}
                resources={res}
                availableVisitorGroups={this.params.availableVisitorGroups}
            />,
        );
    },
    contextChanged: function () {
        if (!this.params.isEnabled) {
            return;
        }

        const shouldEnable = this._currentContext?.capabilities?.isPage
            && this._currentContext?.type === "epi.cms.contentdata";

        if (shouldEnable) {
            this.store.enable();
            this.store.load();
        } else {
            this.store.disable();
        }
    },
    destroy: function () {
        if (!this.params.isEnabled) {
            return;
        }

        this.root.unmount();
    },
});
