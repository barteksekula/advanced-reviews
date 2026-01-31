import type { Meta, StoryObj } from "@storybook/react";
import React from "react";
import { action } from "storybook/actions";

import FakeAdvancedReviewService from "../../.storybook/fake-advanced-review-service";
import resources from "../../.storybook/resources.json";
import { Comment, createStores, PinLocation, Priority } from "../store/review-store";
import Pin from "./pin";

const stores = createStores(new FakeAdvancedReviewService(), resources);
stores.reviewStore.load();

const getReviewLocation = (
    isDone: boolean = false,
    priority: Priority = Priority.Normal,
    lastCommentFromOtherUser: boolean = false,
) => {
    const user = lastCommentFromOtherUser ? stores.reviewStore.currentUser + "1" : stores.reviewStore.currentUser;
    const reviewLocation = new PinLocation(stores.reviewStore, {
        id: "25",
        propertyName: "test",
        isDone: isDone,
        documentRelativePosition: { x: 100, y: 100 },
        priority: priority,
        firstComment: Comment.create(user, "aaaaa aaaaa", stores.reviewStore, new Date("2019-02-03")),
        comments: [],
    });
    return reviewLocation;
};

const meta: Meta<typeof Pin> = {
    title: "Pin",
    component: Pin,
};

export default meta;
type Story = StoryObj<typeof Pin>;

export const Default: Story = {
    render: () => {
        const location = getReviewLocation(false, Priority.Normal, false);
        return (
            <Pin
                location={location}
                showDialog={action("selectLocation")}
                position={location.documentRelativePosition}
            />
        );
    },
};

export const Done: Story = {
    render: () => {
        const reviewLocation = getReviewLocation(true);
        return (
            <Pin
                location={reviewLocation}
                showDialog={action("selectLocation")}
                position={reviewLocation.documentRelativePosition}
            />
        );
    },
};

export const HighPriority: Story = {
    render: () => {
        const reviewLocation = getReviewLocation(false, Priority.Important);
        return (
            <Pin
                location={reviewLocation}
                position={reviewLocation.documentRelativePosition}
                showDialog={action("selectLocation")}
            />
        );
    },
};

export const LowPriority: Story = {
    render: () => {
        const reviewLocation = getReviewLocation(false, Priority.Trivial);
        return (
            <Pin
                location={reviewLocation}
                position={reviewLocation.documentRelativePosition}
                showDialog={action("selectLocation")}
            />
        );
    },
};

export const Updated: Story = {
    render: () => {
        const reviewLocation = getReviewLocation(false, Priority.Normal, true);
        return (
            <Pin
                location={reviewLocation}
                position={reviewLocation.documentRelativePosition}
                showDialog={action("selectLocation")}
            />
        );
    },
};
