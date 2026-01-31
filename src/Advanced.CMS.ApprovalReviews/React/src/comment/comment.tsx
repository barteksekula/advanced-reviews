import "./comment.scss";

import Icon from "@mui/material/Icon";
import classNames from "classnames";
import { inject, observer } from "mobx-react";
import React from "react";

import { DropDownMenu } from "../common/drop-down-menu";
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
                            <DropDownMenu icon="image" title={res.panel.showscreenshot}>
                                <img src={comment.screenshot} alt="screenshot" />
                            </DropDownMenu>
                        </div>
                    )}
                </div>
                <p className={classNames({ amplify, done: isDone })}>{comment.text}</p>
            </div>
        </div>
    );
};

export default inject("resources", "reviewStore")(observer(Comment));
