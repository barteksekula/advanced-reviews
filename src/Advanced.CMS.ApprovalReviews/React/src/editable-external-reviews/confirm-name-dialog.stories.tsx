import type { Meta, StoryObj } from "@storybook/react";
import React from "react";
import { action } from "storybook/actions";

import ConfirmDialog from "./confirm-name-dialog";

const meta: Meta<typeof ConfirmDialog> = {
    title: "External editable reviews/Confirm user name dialog",
    component: ConfirmDialog,
};

export default meta;
type Story = StoryObj<typeof ConfirmDialog>;

export const Default: Story = {
    render: () => <ConfirmDialog onClose={action("onclose")} open={true} initialUserName="reviewer1" />,
};
