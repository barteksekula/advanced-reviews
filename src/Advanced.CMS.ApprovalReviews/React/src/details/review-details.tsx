import "./review-details.scss";

import Button from "@mui/material/Button";
import { reaction } from "mobx";
import { inject, observer } from "mobx-react";
import React, { useEffect, useRef, useState } from "react";
import ScrollArea from "react-scrollbar";

import Comment from "../comment/comment";
import LocationComment from "../location-comment/location-comment";
import ScreenshotDialog from "../screenshot-dialog/screenshot-dialog";
import { IReviewComponentStore, NewPinDto, PinLocation } from "../store/review-store";

interface ReviewDetailsProps {
    iframe?: HTMLIFrameElement;
    reviewStore?: IReviewComponentStore;
    resources?: ReviewResources;
    currentEditLocation: PinLocation;
    onCancel: () => void;
}

const ReviewDetails: React.FC<ReviewDetailsProps> = ({
    iframe,
    reviewStore,
    resources,
    currentEditLocation,
    onCancel,
}) => {
    const [currentPriority, setCurrentPriority] = useState(currentEditLocation.priority);
    const [currentScreenshot, setCurrentScreenshot] = useState<string>(null);
    const [screenshotMode, setScreenshotMode] = useState(false);
    const [currentCommentText, setCurrentCommentText] = useState("");
    const scrollableRef = useRef<any>(null);

    const scrollToBottom = () => {
        setTimeout(() => {
            scrollableRef.current?.scrollBottom();
        }, 0);
    };

    useEffect(() => {
        scrollToBottom();
    }, []);

    useEffect(() => {
        const dispose = reaction(
            () => {
                return currentEditLocation.comments.slice();
            },
            () => {
                scrollToBottom();
            },
        );

        return () => {
            dispose();
        };
    }, [currentEditLocation.comments]);

    const addNewComment = () => {
        reviewStore.addComment(currentCommentText, currentScreenshot).then(() => {
            setScreenshotMode(false);
            setCurrentScreenshot(null);
            setCurrentCommentText("");
        });
    };

    const updateComment = (comment: string, screenshot: string) => {
        setCurrentScreenshot(screenshot);
        setCurrentCommentText(comment);
    };

    const canSave: boolean = currentCommentText.trim() !== "";
    const res = resources!;

    return (
        <div className="review-details">
            <div className="first-comment">
                <Comment comment={currentEditLocation.firstComment} amplify />
            </div>
            <ScrollArea speed={0.8} className="comments-list" horizontal={false} ref={scrollableRef}>
                {currentEditLocation.comments.map((comment, idx) => (
                    <Comment key={idx} comment={comment} />
                ))}
            </ScrollArea>
            {!currentEditLocation.isDone && (
                <>
                    <LocationComment
                        value={currentCommentText}
                        currentScreenshot={currentScreenshot}
                        onToggle={() => setScreenshotMode(!screenshotMode)}
                        onChange={(comment, screenshot) => {
                            updateComment(comment, screenshot);
                        }}
                        allowScreenshotAttachments={reviewStore.options.allowScreenshotAttachments}
                    />
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
                    <div className="actions">
                        <Button onClick={onCancel}>{res.dialog.close}</Button>
                        <Button disabled={!canSave} onClick={addNewComment}>
                            {res.dialog.addcomment}
                        </Button>
                    </div>
                </>
            )}
        </div>
    );
};

export default inject("reviewStore", "resources")(observer(ReviewDetails));
