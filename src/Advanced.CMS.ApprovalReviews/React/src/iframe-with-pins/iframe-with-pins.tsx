import Button from "@mui/material/Button";
import Snackbar from "@mui/material/Snackbar";
import { inject, observer } from "mobx-react";
import React, { useEffect, useMemo, useState } from "react";
import { createPortal } from "react-dom";

import IframeOverlay from "../iframe-overlay/iframe-overlay";
import NewReviewDialog from "../new-review-dialog/new-review-dialog";
import PinCollection from "../pin-collection/pin-collection";
import createPositionCalculator from "../position-calculator/position-calculator";
import createPreviewFrame, { PreviewFrame } from "../preview-frame/preview-frame";
import ReviewsSlidingPanel from "../reviews-sliding-panel/reviews-sliding-panel";
import { Dimensions, IReviewComponentStore, NewPinDto, PinLocation } from "../store/review-store";

interface IframeWithPinsProps {
    iframe: HTMLIFrameElement;
    reviewStore?: IReviewComponentStore;
    external?: boolean;
}

const sameSize = (a: Dimensions, b: Dimensions) => a.x === b.x && a.y === b.y;

const IframeWithPins: React.FC<IframeWithPinsProps> = ({ iframe, reviewStore, external }) => {
    const [frame, setFrame] = useState<PreviewFrame>(null);
    const [newLocation, setNewLocation] = useState<PinLocation>(null);
    const [documentSize, setDocumentSize] = useState<Dimensions>({ x: 0, y: 0 });
    const [layoutVersion, setLayoutVersion] = useState(0);
    const [showReviewIntro, setShowReviewIntro] = useState<boolean>(
        reviewStore.reviewLocations.length === 0 && localStorage.getItem("reviewIntro") !== "false",
    );

    useEffect(() => {
        const previewFrame = createPreviewFrame(iframe, external);
        const update = () => {
            const displayNames = previewFrame.getDisplayNames();
            const mapping = reviewStore.propertyNameMapping ?? {};
            if (Object.keys(displayNames).some((name) => !mapping[name])) {
                reviewStore.updateDisplayNamesDictionary({ ...displayNames, ...mapping });
            }

            const size = previewFrame.getDocumentSize();
            setDocumentSize((current) => (sameSize(current, size) ? current : size));
            setLayoutVersion((version) => version + 1);
        };

        previewFrame.subscribe(update);
        update();
        setFrame(previewFrame);

        return () => {
            previewFrame.dispose();
        };
    }, [iframe, external, reviewStore]);

    const positionCalculator = useMemo(
        () => createPositionCalculator(documentSize, frame ?? undefined),
        [documentSize, frame, layoutVersion],
    );

    if (!frame) {
        return null;
    }

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

    return (
        <>
            {reviewStore.filter.reviewMode && (
                <IframeOverlay
                    frame={frame}
                    reviewLocationCreated={(location) => setNewLocation(location)}
                    external={external}
                >
                    <PinCollection newLocation={newLocation} positionCalculator={positionCalculator} />
                </IframeOverlay>
            )}
            <ReviewsSlidingPanel frame={frame} />
            {newLocation && (
                <NewReviewDialog
                    currentEditLocation={newLocation}
                    frame={frame}
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
