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

class PinNavigator extends React.Component<PinNavigatorProps, any> {
    showReview(incrementBy: number): void {
        const { editedPinLocation, reviewLocations } = this.props.reviewStore!;
        let reviewIndex = reviewLocations.indexOf(editedPinLocation) + incrementBy;
        if (reviewIndex >= reviewLocations.length) {
            reviewIndex = 0;
        } else if (reviewIndex < 0) {
            reviewIndex = reviewLocations.length - 1;
        }
        this.props.reviewStore.selectedPinLocation = this.props.reviewStore.editedPinLocation =
            reviewLocations[reviewIndex];
    }

    render() {
        const { reviewLocations } = this.props.reviewStore!;
        const res = this.props.resources!;

        const currentItemIndex = reviewLocations.indexOf(this.props.reviewStore.editedPinLocation);

        const isNextEnabled = currentItemIndex < reviewLocations.length - 1;
        let nextTitle = "next";
        if (isNextEnabled) {
            nextTitle =
                nextTitle +
                ": " +
                this.props.reviewStore.resolvePropertyDisplayName(reviewLocations[currentItemIndex + 1].propertyName);
        }

        const isPrevEnabled = currentItemIndex > 0;
        let prevTitle = "prev";
        if (isPrevEnabled) {
            prevTitle =
                prevTitle +
                ": " +
                this.props.reviewStore.resolvePropertyDisplayName(reviewLocations[currentItemIndex - 1].propertyName);
        }

        return (
            <div className="pin-navigator">
                {reviewLocations.length > 1 && (
                    <>
                        <IconButton
                            className="next-prev-icon"
                            title={prevTitle}
                            disabled={!isPrevEnabled}
                            onClick={() => this.showReview(-1)}
                        >
                            <ChevronLeftIcon />
                        </IconButton>
                        <span className="pager">
                            {currentItemIndex + 1} / {reviewLocations.length}
                        </span>
                        <IconButton
                            className="next-prev-icon"
                            title={nextTitle}
                            onClick={() => this.showReview(1)}
                            disabled={!isNextEnabled}
                        >
                            <ChevronRightIcon />
                        </IconButton>
                    </>
                )}
                <IconButton
                    className="next-prev-icon"
                    title={res.panel.gobacktolist}
                    onClick={() => (this.props.reviewStore.editedPinLocation = null)}
                >
                    <ListIcon />
                </IconButton>
            </div>
        );
    }
}

export default inject("reviewStore", "resources")(observer(PinNavigator));
