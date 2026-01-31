import type { Meta, StoryObj } from "@storybook/react";
import React from "react";

// Simple test component
const TestComponent = () => {
    return <span>Hello Storybook!</span>;
};

const meta: Meta<typeof TestComponent> = {
    title: "Test/Simple Component",
    component: TestComponent,
};

export default meta;
type Story = StoryObj<typeof TestComponent>;

export const Default: Story = {
    render: () => <TestComponent />,
};
