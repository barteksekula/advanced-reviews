import type { Meta, StoryObj } from "@storybook/react";
import React from "react";

import res from "../../.storybook/externalResources.json";
import ExternalReviewWidgetContent, { ExternalReviewWidgetContentProps } from "./external-review-manage-links";
import FakeReviewLinksStore from "./fake-review-links-store";

const visitorGroups = [
    { name: "Visitor group 1", id: "1" },
    { name: "Visitor group 2", id: "2" },
];

const getDefaultProps = (store: FakeReviewLinksStore): ExternalReviewWidgetContentProps => ({
    store: store,
    editableLinksEnabled: true,
    resources: res,
    availableVisitorGroups: visitorGroups,
    pinCodeSecurityEnabled: false,
    pinCodeLength: 4,
    prolongDays: 5,
});

const meta: Meta<typeof ExternalReviewWidgetContent> = {
    title: "External reviews/Review component",
    component: ExternalReviewWidgetContent,
};

export default meta;
type Story = StoryObj<typeof ExternalReviewWidgetContent>;

export const Default: Story = {
    render: () => {
        const store = new FakeReviewLinksStore();
        store.initialMailSubject = "Review request";
        store.initialEditMailMessage =
            "EDIT: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed egestas rutrum lacus eget dapibus. Aenean eleifend commodo felis vitae convallis.";
        store.initialViewMailMessage =
            "VIEW: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed egestas rutrum lacus eget dapibus. Aenean eleifend commodo felis vitae convallis.";
        return <ExternalReviewWidgetContent {...getDefaultProps(store)} />;
    },
};

export const Empty: Story = {
    render: () => {
        const store = new FakeReviewLinksStore();
        store.links = [];
        return <ExternalReviewWidgetContent {...getDefaultProps(store)} />;
    },
};

export const OnlyViewLinks: Story = {
    render: () => {
        const store = new FakeReviewLinksStore();
        store.links = [];
        return <ExternalReviewWidgetContent {...getDefaultProps(store)} editableLinksEnabled={false} />;
    },
};

export const WithPINSecurity: Story = {
    render: () => {
        const store = new FakeReviewLinksStore();
        return (
            <ExternalReviewWidgetContent {...getDefaultProps(store)} pinCodeSecurityEnabled={true} pinCodeLength={4} />
        );
    },
};

export const WithRequiredPINSecurity: Story = {
    render: () => {
        const store = new FakeReviewLinksStore();
        return (
            <ExternalReviewWidgetContent
                {...getDefaultProps(store)}
                pinCodeSecurityEnabled={true}
                pinCodeSecurityRequired={true}
                pinCodeLength={4}
            />
        );
    },
};

export const WithPINSecurityNotEnabled: Story = {
    render: () => {
        const store = new FakeReviewLinksStore();
        return (
            <ExternalReviewWidgetContent {...getDefaultProps(store)} pinCodeSecurityEnabled={false} pinCodeLength={4} />
        );
    },
};
