import "./comment.scss";

import Icon from "@mui/material/Icon";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import classNames from "classnames";
import { inject, observer } from "mobx-react";
import React, { useState } from "react";

import { Comment as CommentItem, IReviewComponentStore } from "../store/review-store";

interface CommentProps {
    reviewStore?: IReviewComponentStore;
    resources?: ReviewResources;
    comment: CommentItem;
    amplify?: boolean;
    isImportant?: boolean;
    isDone?: boolean;
}

const Comment: React.FC<CommentProps> = ({ reviewStore, resources, comment, amplify, isImportant, isDone }) => {
    const { getUserAvatarUrl } = reviewStore!;
    const res = resources!;
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

    const handlePopoverOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handlePopoverClose = () => {
        setAnchorEl(null);
    };

    return (
        <div className="comment">
            <div className="avatar">
                <img src={getUserAvatarUrl(comment.author)} alt={comment.author} />
            </div>
            <div className="message">
                <div>
                    {isImportant && <Icon title={res.priority.important}>priority_high</Icon>}
                    <span className="author" title={comment.author}>
                        {comment.author}
                    </span>
                    <span className="date" title={comment.formattedDate}>
                        {comment.userFriendlyDate}
                    </span>
                    {comment.screenshot && (
                        <div className="screenshot">
                            <IconButton
                                size="small"
                                title={res.panel.showscreenshot}
                                onClick={handlePopoverOpen}
                            >
                                <Icon>image</Icon>
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
                                <img src={comment.screenshot} alt="screenshot" style={{ maxWidth: "600px", display: "block" }} />
                            </Popover>
                        </div>
                    )}
                </div>
                <p className={classNames({ amplify, done: isDone })}>{comment.text}</p>
            </div>
        </div>
    );
};

export default inject("resources", "reviewStore")(observer(Comment));
