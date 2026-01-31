import "./new-review-dialog.scss";

import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import { inject, observer } from "mobx-react";
import React, { useState } from "react";

import { ContextMenu } from "../common/context-menu";
import LocationComment from "../location-comment/location-comment";
import ScreenshotDialog from "../screenshot-dialog/screenshot-dialog";
import priorityIconMappings from "../store/priority-icon-mappings";
import { IReviewComponentStore, NewPinDto, PinLocation, Priority } from "../store/review-store";

interface NewReviewDialogProps {
    iframe?: HTMLIFrameElement;
    reviewStore?: IReviewComponentStore;
    resources?: ReviewResources;
    currentEditLocation: PinLocation;

    onCloseDialog(action: string, state: NewPinDto): void;
}

const NewReviewDialog: React.FC<NewReviewDialogProps> = ({
    iframe,
    reviewStore,
    resources,
    currentEditLocation,
    onCloseDialog,
}) => {
    const [currentPriority, setCurrentPriority] = useState(currentEditLocation.priority);
    const [currentScreenshot, setCurrentScreenshot] = useState<string>(null);
    const [screenshotMode, setScreenshotMode] = useState(false);
    const [currentCommentText, setCurrentCommentText] = useState("");

    const updateComment = (comment: string, screenshot: string) => {
        setCurrentScreenshot(screenshot);
        setCurrentCommentText(comment);
    };

    const getState = (): NewPinDto => ({
        currentPriority,
        currentScreenshot,
        screenshotMode,
        currentCommentText,
    });

    const res = resources!;

    const options = Object.keys(Priority).map((priority) => {
        return {
            name: res.priority[priority.toLowerCase()],
            icon: priorityIconMappings[priority],
            onSelected: () => {
                setCurrentPriority(Priority[priority]);
            },
        };
    });

    const canSave: boolean = currentCommentText.trim() !== "";

    return (
        <>
            <Dialog className="review-dialog" open={true} onClose={() => onCloseDialog("cancel", getState())}>
                <DialogTitle>
                    <div className="header">
                        <div className="left-align">
                            {reviewStore.resolvePropertyDisplayName(currentEditLocation.propertyName) ||
                                res.dialog.reviewedit}
                        </div>
                        <div className="review-actions">
                            <ContextMenu
                                icon={priorityIconMappings[currentPriority]}
                                title={res.dialog.changepriority}
                                menuItems={options}
                            />
                        </div>
                    </div>
                </DialogTitle>
                <DialogContent>
                    <div className="dialog-grid">
                        <LocationComment
                            value={currentCommentText}
                            currentScreenshot={currentScreenshot}
                            onToggle={() => setScreenshotMode(!screenshotMode)}
                            onChange={(comment, screenshot) => {
                                updateComment(comment, screenshot);
                            }}
                            allowScreenshotAttachments={reviewStore.options.allowScreenshotAttachments}
                        />
                    </div>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => onCloseDialog("cancel", getState())}>{res.dialog.close}</Button>
                    <Button variant="contained" onClick={() => onCloseDialog("save", getState())} disabled={!canSave}>
                        {res.dialog.save}
                    </Button>
                </DialogActions>
            </Dialog>
            {screenshotMode && (
                <ScreenshotDialog
                    maxWidth={500}
                    maxHeight={300}
                    iframe={iframe}
                    propertyName={currentEditLocation.propertyName}
                    documentRelativePosition={currentEditLocation.documentRelativePosition}
                    documentSize={currentEditLocation.documentSize}
                    onImageSelected={(output) => {
                        setCurrentScreenshot(output);
                    }}
                    toggle={() => setScreenshotMode(!screenshotMode)}
                />
            )}
        </>
    );
};

export default inject("reviewStore", "resources")(observer(NewReviewDialog));
