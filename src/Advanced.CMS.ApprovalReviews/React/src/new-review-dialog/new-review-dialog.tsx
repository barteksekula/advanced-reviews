import "./new-review-dialog.scss";

import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Icon from "@mui/material/Icon";
import IconButton from "@mui/material/IconButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import { inject, observer } from "mobx-react";
import React, { useState } from "react";

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
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
    };

    const handlePriorityChange = (priority: Priority) => {
        setCurrentPriority(priority);
        handleMenuClose();
    };

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


    const canSave: boolean = currentCommentText.trim() !== "";

    return (
        <>
            <Dialog
                className="review-dialog"
                open={true}
                onClose={() => onCloseDialog("cancel", getState())}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    <div className="dialog-title-header">
                        <div className="dialog-title-text">
                            {reviewStore.resolvePropertyDisplayName(currentEditLocation.propertyName) ||
                                res.dialog.reviewedit}
                        </div>
                        <div>
                            <IconButton title={res.dialog.changepriority} onClick={handleMenuOpen}>
                                <Icon>{priorityIconMappings[currentPriority]}</Icon>
                            </IconButton>
                            <Menu
                                anchorEl={anchorEl}
                                open={Boolean(anchorEl)}
                                onClose={handleMenuClose}
                                anchorOrigin={{
                                    vertical: "bottom",
                                    horizontal: "right",
                                }}
                                transformOrigin={{
                                    vertical: "top",
                                    horizontal: "right",
                                }}
                            >
                                {Object.keys(Priority).map((priority) => (
                                    <MenuItem key={priority} onClick={() => handlePriorityChange(Priority[priority])}>
                                        <ListItemIcon>
                                            <Icon>{priorityIconMappings[priority]}</Icon>
                                        </ListItemIcon>
                                        <ListItemText>{res.priority[priority.toLowerCase()]}</ListItemText>
                                    </MenuItem>
                                ))}
                            </Menu>
                        </div>
                    </div>
                </DialogTitle>
                <DialogContent>
                    <div className="dialog-content-container">
                        {reviewStore.options.allowScreenshotAttachments && (
                            <div className="screenshot-controls">
                                <IconButton
                                    color={currentScreenshot ? "primary" : "default"}
                                    title={currentScreenshot ? res.panel.showscreenshot : res.panel.attachscreenshot}
                                    onClick={() => setScreenshotMode(!screenshotMode)}
                                >
                                    <Icon>{currentScreenshot ? "check_circle" : "add_photo_alternate"}</Icon>
                                </IconButton>
                                <span
                                    className="screenshot-label"
                                    onClick={() => setScreenshotMode(!screenshotMode)}
                                >
                                    {currentScreenshot ? res.panel.showscreenshot : res.panel.attachscreenshot}
                                </span>
                                {currentScreenshot && (
                                    <IconButton
                                        size="small"
                                        title={res.panel.removescreenshot}
                                        onClick={() => updateComment(currentCommentText, null)}
                                    >
                                        <Icon fontSize="small">close</Icon>
                                    </IconButton>
                                )}
                            </div>
                        )}
                        <LocationComment
                            value={currentCommentText}
                            currentScreenshot={currentScreenshot}
                            onToggle={() => setScreenshotMode(!screenshotMode)}
                            onChange={(comment, screenshot) => {
                                updateComment(comment, screenshot);
                            }}
                            allowScreenshotAttachments={false}
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
