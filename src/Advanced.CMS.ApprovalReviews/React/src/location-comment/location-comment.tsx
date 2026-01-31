import "./location-comment.scss";

import ImageIcon from "@mui/icons-material/Image";
import RemoveIcon from "@mui/icons-material/Remove";
import IconButton from "@mui/material/IconButton";
import TextField from "@mui/material/TextField";
import { inject } from "mobx-react";
import React, { useEffect, useRef } from "react";

import { DropDownMenu } from "../common/drop-down-menu";

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

    useEffect(() => {
        if (textFieldRef.current) {
            textFieldRef.current.focus();
        }
    });

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
                            <DropDownMenu icon="image" title={resources.panel.showscreenshot}>
                                <img src={props.currentScreenshot} alt="screenshot" />
                            </DropDownMenu>
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
