import Button from "@mui/material/Button";
import Snackbar from "@mui/material/Snackbar";
import { inject, observer } from "mobx-react";
import React from "react";

import IframeOverlay from "../iframe-overlay/iframe-overlay";
import NewReviewDialog from "../new-review-dialog/new-review-dialog";
import PinCollection from "../pin-collection/pin-collection";
import PositionCalculator from "../position-calculator/position-calculator";
import ReviewsSlidingPanel from "../reviews-sliding-panel/reviews-sliding-panel";
import { Dimensions, IReviewComponentStore, NewPinDto, PinLocation } from "../store/review-store";

interface IframeState {
    newLocation: PinLocation;
    documentSize: Dimensions;
}

interface IframeWithPinsProps {
    iframe: HTMLIFrameElement;
    reviewStore?: IReviewComponentStore;
    external?: boolean;
}

@inject("reviewStore")
@observer
export default class IframeWithPins extends React.Component<IframeWithPinsProps, IframeState> {
    constructor(props: IframeWithPinsProps) {
        super(props);

        this.state = {
            newLocation: null,
            documentSize: this.getIframeDimensions(),
        };
    }

    private getIframeDimensions() {
        const iframeDocumentBody = this.props.iframe.contentDocument.body;

        return { x: iframeDocumentBody.offsetWidth, y: iframeDocumentBody.offsetHeight };
    }

    private updateDimensions() {
        this.setState({ documentSize: this.getIframeDimensions() });
    }

    private loadIframe() {
        this.props.iframe.contentWindow.addEventListener("resize", this.updateDimensions.bind(this));
        this.props.iframe.contentWindow.addEventListener("beforeunload", this.unloadIframe.bind(this));
        this.updateDimensions();
    }

    private unloadIframe() {
        this.props.iframe.contentWindow.removeEventListener("resize", this.updateDimensions.bind(this));
        this.props.iframe.contentWindow.removeEventListener("beforeunload", this.unloadIframe.bind(this));
    }

    componentDidMount() {
        this.props.iframe.addEventListener("load", this.loadIframe.bind(this));
        this.loadIframe();
    }

    componentWillUnmount() {
        this.props.iframe.removeEventListener("load", this.loadIframe.bind(this));
    }

    onCloseDialog(action: string, state: NewPinDto): void {
        if (action !== "save") {
            this.setState({
                newLocation: null,
            });
            return;
        }

        this.props.reviewStore
            .save(state, this.state.newLocation)
            .then((createdLocation) => {
                this.setState({
                    newLocation: null,
                });
                // show the pin details only if there's a different pin open currently
                if (this.props.reviewStore.editedPinLocation) {
                    this.props.reviewStore.editedPinLocation = createdLocation;
                }
            })
            .catch((e) => {
                //TODO: handle server exceptions
                alert(e.message);
            });

        //TODO: show screenshot after save this.setState({ isScreenshotMode: true });
    }

    onIntroClose = (reason): void => {
        if (reason !== "action") {
            return;
        }
        localStorage.setItem("reviewIntro", "false");
    };

    render() {
        const showReviewIntro: boolean =
            this.props.reviewStore.reviewLocations.length === 0 && localStorage.getItem("reviewIntro") !== "false";

        const positionCalculator = new PositionCalculator(
            this.state.documentSize,
            this.props.external,
            this.props.iframe.contentDocument,
        );

        return (
            <>
                {this.props.reviewStore.filter.reviewMode && (
                    <IframeOverlay
                        iframe={this.props.iframe}
                        reviewLocationCreated={(location) => this.setState({ newLocation: location })}
                        external={this.props.external}
                    >
                        <PinCollection newLocation={this.state.newLocation} positionCalculator={positionCalculator} />
                        {showReviewIntro && (
                            <Snackbar
                                open={true}
                                autoHideDuration={10000}
                                onClose={this.onIntroClose}
                                message="You are now in content review mode. Click on text to create new review entry."
                                action={
                                    <Button color="secondary" size="small" onClick={this.onIntroClose}>
                                        Do not show this again
                                    </Button>
                                }
                            />
                        )}
                    </IframeOverlay>
                )}
                <ReviewsSlidingPanel iframe={this.props.iframe} />
                {this.state.newLocation && (
                    <NewReviewDialog
                        currentEditLocation={this.state.newLocation}
                        iframe={this.props.iframe}
                        onCloseDialog={(action, state) => this.onCloseDialog(action, state)}
                    />
                )}
            </>
        );
    }
}
