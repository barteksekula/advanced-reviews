import TextField from "@mui/material/TextField";
import type { Meta, StoryObj } from "@storybook/react";
import { Provider } from "mobx-react";
import React, { useEffect, useState } from "react";

import FakeAdvancedReviewService from "../../.storybook/fake-advanced-review-service";
import resources from "../../.storybook/resources.json";
import { createStores } from "../store/review-store";
import IframeWithPins from "./iframe-with-pins";

const stores = createStores(new FakeAdvancedReviewService(), resources);

function Component({ initialLocale = "en" }) {
    const [text, setText] = useState("Lina");
    const [iframeLoaded, setIframeLoaded] = useState(false);
    const [anchorElement, setAnchorElement] = useState(null);

    useEffect(() => {
        stores.reviewStore.currentUser = text;
        stores.reviewStore.currentLocale = initialLocale;

        setTimeout(() => {
            setIframeLoaded(true);
        }, 1000);
    });

    return (
        <div>
            <div
                id="iframeWrapper"
                style={{
                    width: "100%",
                    height: "800px",
                    position: "absolute",
                    top: "0",
                    overflowY: "scroll",
                    overflowX: "auto",
                }}
            >
                <iframe
                    id="iframe"
                    ref={setAnchorElement}
                    style={{ width: "100%", height: "985px" }}
                    src="/fake_OPE.html"
                />
            </div>
            {!!anchorElement && iframeLoaded && <IframeWithPins iframe={anchorElement} />}
            <div className="user-picker">
                <TextField
                    label="Current user"
                    value={text}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => setText(e.target.value)}
                />
            </div>
        </div>
    );
}

const meta: Meta<typeof IframeWithPins> = {
    title: "Dojo component",
    component: IframeWithPins,
};

export default meta;
type Story = StoryObj<typeof IframeWithPins>;

export const Default: Story = {
    render: () => {
        stores.reviewStore.load();
        return (
            <Provider {...stores}>
                <Component />
            </Provider>
        );
    },
};

export const EmptyList: Story = {
    render: () => {
        stores.reviewStore.reviewLocations = [];
        return (
            <Provider {...stores}>
                <Component />
            </Provider>
        );
    },
};

export const SwedishLocale: Story = {
    render: () => {
        stores.reviewStore.load();
        return (
            <Provider {...stores}>
                <Component initialLocale="sv" />
            </Provider>
        );
    },
};
