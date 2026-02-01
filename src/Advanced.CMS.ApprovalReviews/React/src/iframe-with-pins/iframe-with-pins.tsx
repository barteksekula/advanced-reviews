import Button from "@mui/material/Button";
import Snackbar from "@mui/material/Snackbar";
import { inject, observer } from "mobx-react";
import React, { useCallback, useEffect, useMemo, useState } from "react";
import { createPortal } from "react-dom";

import IframeOverlay from "../iframe-overlay/iframe-overlay";
import NewReviewDialog from "../new-review-dialog/new-review-dialog";
import PinCollection from "../pin-collection/pin-collection";
import createPositionCalculator from "../position-calculator/position-calculator";
import ReviewsSlidingPanel from "../reviews-sliding-panel/reviews-sliding-panel";
import { Dimensions, IReviewComponentStore, NewPinDto, PinLocation } from "../store/review-store";

interface IframeWithPinsProps {
    iframe: HTMLIFrameElement;
    reviewStore?: IReviewComponentStore;
    external?: boolean;
}

const IframeWithPins: React.FC<IframeWithPinsProps> = ({ iframe, reviewStore, external }) => {
    const getIframeDimensions = useCallback(() => {
        const iframeDocumentBody = iframe.contentDocument.body;
        return { x: iframeDocumentBody.offsetWidth, y: iframeDocumentBody.offsetHeight };
    }, [iframe]);

    const [newLocation, setNewLocation] = useState<PinLocation>(null);
    const [documentSize, setDocumentSize] = useState<Dimensions>(getIframeDimensions());
    const [showReviewIntro, setShowReviewIntro] = useState<boolean>(
        reviewStore.reviewLocations.length === 0 && localStorage.getItem("reviewIntro") !== "false",
    );

    const updateDimensions = useCallback(() => {
        setDocumentSize(getIframeDimensions());
    }, [getIframeDimensions]);

    const loadIframe = useCallback(() => {
        iframe.contentWindow.addEventListener("resize", updateDimensions);
        iframe.contentWindow.addEventListener("beforeunload", unloadIframe);
        updateDimensions();
    }, [iframe, updateDimensions]);

    const unloadIframe = useCallback(() => {
        iframe.contentWindow.removeEventListener("resize", updateDimensions);
        iframe.contentWindow.removeEventListener("beforeunload", unloadIframe);
    }, [iframe, updateDimensions]);

    useEffect(() => {
        iframe.addEventListener("load", loadIframe);
        loadIframe();

        return () => {
            iframe.removeEventListener("load", loadIframe);
        };
    }, [iframe, loadIframe]);

    const onCloseDialog = (action: string, state: NewPinDto): void => {
        if (action !== "save") {
            setNewLocation(null);
            return;
        }

        reviewStore
            .save(state, newLocation)
            .then((createdLocation) => {
                setNewLocation(null);
                // show the pin details only if there's a different pin open currently
                if (reviewStore.editedPinLocation) {
                    reviewStore.editedPinLocation = createdLocation;
                }
            })
            .catch((e) => {
                //TODO: handle server exceptions
                alert(e.message);
            });

        //TODO: show screenshot after save this.setState({ isScreenshotMode: true });
    };

    const onIntroClose = (): void => {
        setShowReviewIntro(false);
        localStorage.setItem("reviewIntro", "false");
    };


    const positionCalculator = useMemo(
        () => createPositionCalculator(documentSize, external, iframe.contentDocument),
        [documentSize, external, iframe],
    );

    return (
        <>
            {reviewStore.filter.reviewMode && (
                <IframeOverlay
                    iframe={iframe}
                    reviewLocationCreated={(location) => setNewLocation(location)}
                    external={external}
                >
                    <PinCollection newLocation={newLocation} positionCalculator={positionCalculator} />
                </IframeOverlay>
            )}
            <ReviewsSlidingPanel iframe={iframe} />
            {newLocation && (
                <NewReviewDialog
                    currentEditLocation={newLocation}
                    iframe={iframe}
                    onCloseDialog={(action, state) => onCloseDialog(action, state)}
                />
            )}
            {showReviewIntro &&
                createPortal(
                    <Snackbar
                        open={true}
                        autoHideDuration={10000}
                        onClose={onIntroClose}
                        message="You are now in content review mode. Click on text to create new review entry."
                        action={
                            <Button color="secondary" size="small" onClick={onIntroClose}>
                                Do not show this again
                            </Button>
                        }
                        sx={{ zIndex: 999999 }}
                    />,
                    document.body,
                )}
        </>
    );
};

export default inject("reviewStore")(observer(IframeWithPins));
