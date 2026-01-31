import type { Meta, StoryObj } from "@storybook/react";
import React from "react";
import { action } from "storybook/actions";

import res from "../../.storybook/externalResources.json";
import ShareDialog from "./external-review-share-dialog";

const meta: Meta<typeof ShareDialog> = {
    title: "External reviews/Share dialog",
    component: ShareDialog,
};

export default meta;
type Story = StoryObj<typeof ShareDialog>;

export const Default: Story = {
    render: () => (
        <ShareDialog
            onClose={action("onclose")}
            open={true}
            initialSubject="Review request"
            initialMessage="Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi non lorem efficitur, luctus tortor non, placerat orci. Nunc nec mollis est, mattis ultrices quam. Etiam non commodo felis."
            resources={res}
        />
    ),
};
