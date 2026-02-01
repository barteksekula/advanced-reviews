import "./location-comment.scss";

import ImageIcon from "@mui/icons-material/Image";
import RemoveIcon from "@mui/icons-material/Remove";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import TextField from "@mui/material/TextField";
import { inject } from "mobx-react";
import React, { useEffect, useRef, useState } from "react";

interface LocationCommentProps {
    currentScreenshot: string;
    value: string;
    resources?: ReviewResources;
    onToggle: () => void;
    onChange: (comment: string, screenshot: string) => void;
    allowScreenshotAttachments: boolean;
}

const LocationComment = inject("resources")((props: LocationCommentProps) => {
    const textFieldRef = useRef<HTMLInputElement>(null);
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

    useEffect(() => {
        if (textFieldRef.current) {
            textFieldRef.current.focus();
        }
    });

    const handlePopoverOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handlePopoverClose = () => {
        setAnchorEl(null);
    };

    const resources = props.resources!;

    return (
        <>
            <TextField
                className="location-comment-field"
                label={`${resources.dialog.addcomment}...`}
                multiline
                rows={4}
                fullWidth
                inputRef={textFieldRef}
                value={props.value}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                    props.onChange(e.target.value, props.currentScreenshot)
                }
            />
            {props.allowScreenshotAttachments && (
                <>
                    {!props.currentScreenshot && (
                        <IconButton
                            className="attach-screenshot"
                            title={resources.panel.attachscreenshot}
                            onClick={() => props.onToggle()}
                        >
                            <ImageIcon />
                        </IconButton>
                    )}
                    {props.currentScreenshot && (
                        <div className="attach-screenshot">
                            <IconButton
                                title={resources.panel.showscreenshot}
                                onClick={handlePopoverOpen}
                            >
                                <ImageIcon />
                            </IconButton>
                            <Popover
                                open={Boolean(anchorEl)}
                                anchorEl={anchorEl}
                                onClose={handlePopoverClose}
                                anchorOrigin={{
                                    vertical: "bottom",
                                    horizontal: "left",
                                }}
                                transformOrigin={{
                                    vertical: "top",
                                    horizontal: "left",
                                }}
                            >
                                <img src={props.currentScreenshot} alt="screenshot" style={{ maxWidth: "600px", display: "block" }} />
                            </Popover>
                            <IconButton
                                onClick={() => props.onChange(props.value, null)}
                                title={resources.panel.removescreenshot}
                            >
                                <RemoveIcon />
                            </IconButton>
                        </div>
                    )}
                </>
            )}
        </>
    );
});

export default LocationComment;
