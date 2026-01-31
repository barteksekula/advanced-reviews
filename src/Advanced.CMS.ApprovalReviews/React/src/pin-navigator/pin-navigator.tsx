import "./pin-navigator.scss";

import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import ListIcon from "@mui/icons-material/List";
import IconButton from "@mui/material/IconButton";
import { inject, observer } from "mobx-react";
import React from "react";

import { IReviewComponentStore } from "../store/review-store";

interface PinNavigatorProps {
    resources?: ReviewResources;
    reviewStore?: IReviewComponentStore;
}

const PinNavigator: React.FC<PinNavigatorProps> = ({ resources, reviewStore }) => {
    const showReview = (incrementBy: number): void => {
        const { editedPinLocation, reviewLocations } = reviewStore!;
        let reviewIndex = reviewLocations.indexOf(editedPinLocation) + incrementBy;
        if (reviewIndex >= reviewLocations.length) {
            reviewIndex = 0;
        } else if (reviewIndex < 0) {
            reviewIndex = reviewLocations.length - 1;
        }
        reviewStore.selectedPinLocation = reviewStore.editedPinLocation = reviewLocations[reviewIndex];
    };

    const { reviewLocations } = reviewStore!;
    const res = resources!;

    const currentItemIndex = reviewLocations.indexOf(reviewStore.editedPinLocation);

    const isNextEnabled = currentItemIndex < reviewLocations.length - 1;
    let nextTitle = "next";
    if (isNextEnabled) {
        nextTitle =
            nextTitle +
            ": " +
            reviewStore.resolvePropertyDisplayName(reviewLocations[currentItemIndex + 1].propertyName);
    }

    const isPrevEnabled = currentItemIndex > 0;
    let prevTitle = "prev";
    if (isPrevEnabled) {
        prevTitle =
            prevTitle +
            ": " +
            reviewStore.resolvePropertyDisplayName(reviewLocations[currentItemIndex - 1].propertyName);
    }

    return (
        <div className="pin-navigator">
            {reviewLocations.length > 1 && (
                <>
                    <IconButton
                        className="next-prev-icon"
                        title={prevTitle}
                        disabled={!isPrevEnabled}
                        onClick={() => showReview(-1)}
                    >
                        <ChevronLeftIcon />
                    </IconButton>
                    <span className="pager">
                        {currentItemIndex + 1} / {reviewLocations.length}
                    </span>
                    <IconButton
                        className="next-prev-icon"
                        title={nextTitle}
                        onClick={() => showReview(1)}
                        disabled={!isNextEnabled}
                    >
                        <ChevronRightIcon />
                    </IconButton>
                </>
            )}
            <IconButton
                className="next-prev-icon"
                title={res.panel.gobacktolist}
                onClick={() => (reviewStore.editedPinLocation = null)}
            >
                <ListIcon />
            </IconButton>
        </div>
    );
};

export default inject("reviewStore", "resources")(observer(PinNavigator));
