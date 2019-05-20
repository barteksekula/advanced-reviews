import React from "react";
import { storiesOf } from "@storybook/react";
import ShareDialog from "./external-review-share-dialog";
import { action } from "@storybook/addon-actions";

storiesOf("External reviews/Share dialog", module).add("default", () => {
    return (
        <ShareDialog
            onClose={action("onclose")}
            open={true}
            initialMessage="Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi non lorem efficitur, luctus tortor non, placerat orci. Nunc nec mollis est, mattis ultrices quam. Etiam non commodo felis."
        />
    );
});
