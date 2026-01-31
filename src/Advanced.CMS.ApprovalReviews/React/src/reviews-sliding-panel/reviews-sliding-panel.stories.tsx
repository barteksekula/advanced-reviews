import type { Meta, StoryObj } from "@storybook/react";
import { Provider } from "mobx-react";
import React from "react";

import FakeAdvancedReviewService from "../../.storybook/fake-advanced-review-service";
import resources from "../../.storybook/resources.json";
import { createStores } from "../store/review-store";
import SlidingPanel from "./reviews-sliding-panel";

const stores = createStores(new FakeAdvancedReviewService(), resources);

const meta: Meta<typeof SlidingPanel> = {
    title: "Sliding panel",
    component: SlidingPanel,
};

export default meta;
type Story = StoryObj<typeof SlidingPanel>;

export const Default: Story = {
    render: () => {
        stores.reviewStore.load();
        return (
            <Provider {...stores}>
                <SlidingPanel />
            </Provider>
        );
    },
};
