import "./screenshot-dialog.scss";

import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import { inject, observer } from "mobx-react";
import React, { useEffect, useMemo, useRef, useState } from "react";
import ReactCrop, { Crop } from "react-image-crop";

import DrawablePreview from "../drawable-preview/drawable-preview";
import { PreviewFrame } from "../preview-frame/preview-frame";
import { Dimensions } from "../store/review-store";

interface ScreenshotPickerProps {
    frame: PreviewFrame;
    propertyName?: string;
    documentRelativePosition?: Dimensions;
    documentSize?: Dimensions;
    resources?: ReviewResources;
    onImageSelected: (string) => void;
    toggle: () => void;
    maxWidth: number;
    maxHeight: number;
}

interface ResizeResult {
    image: string;
    width: number;
    height: number;
}

enum Mode {
    Default,
    Crop,
    Highlight,
}

function resize(base64Str: string, maxWidth: number, maxHeight: number): Promise<ResizeResult> {
    return new Promise((resolve) => {
        const img = new Image();
        img.src = base64Str;
        img.onload = function () {
            const canvas = document.createElement("canvas");
            let width = img.width;
            let height = img.height;

            if (width > height) {
                if (width > maxWidth) {
                    height *= maxWidth / width;
                    width = maxWidth;
                }
            } else {
                if (height > maxHeight) {
                    width *= maxHeight / height;
                    height = maxHeight;
                }
            }
            canvas.width = width;
            canvas.height = height;
            const ctx = canvas.getContext("2d");
            ctx.drawImage(img, 0, 0, width, height);
            resolve({
                image: canvas.toDataURL(),
                width: width,
                height: height,
            });
        };
    });
}

function getDefaultCrop(
    propertyName: string | undefined,
    frame: PreviewFrame,
    documentRelativePosition: Dimensions,
    documentSize: Dimensions,
) {
    const defaultCrop: Crop = {
        unit: "%",
        width: 50,
        height: 50,
        x: 10,
        y: 10,
    };

    // if user didn't click on the property then try to show default crop on the pin location
    function getDefaultDocumentCrop(position: Dimensions, size: Dimensions): Crop | null {
        const rectangleSize = 20;

        if (!position || !size) {
            return null;
        }

        if (size.x === 0 || size.y === 0) {
            return null;
        }

        const defaultDocumentCrop: Crop = { unit: "%" };
        defaultDocumentCrop.width = rectangleSize;
        defaultDocumentCrop.height = rectangleSize;

        // get x and y
        defaultDocumentCrop.x = (position.x * 100) / size.x - rectangleSize / 2;
        defaultDocumentCrop.y = (position.y * 100) / size.y - rectangleSize / 2;
        if (defaultDocumentCrop.x < 0) {
            defaultDocumentCrop.x = 0;
        } else if (defaultDocumentCrop.x + rectangleSize > 100) {
            defaultDocumentCrop.x = 100 - rectangleSize;
        }
        if (defaultDocumentCrop.y < 0) {
            defaultDocumentCrop.y = 0;
        } else if (defaultDocumentCrop.y + rectangleSize > 100) {
            defaultDocumentCrop.y = 100 - rectangleSize;
        }

        return defaultDocumentCrop;
    }

    if (frame.isRemote()) {
        return { ...defaultCrop };
    }

    const defaultDocumentCrop = getDefaultDocumentCrop(documentRelativePosition, documentSize) || { ...defaultCrop };

    if (!propertyName) {
        return defaultDocumentCrop;
    }

    const propertyBox = frame.getPropertyBox(propertyName);
    if (!propertyBox) {
        return defaultDocumentCrop;
    }

    const frameSize = frame.getDocumentSize();
    if (frameSize.x === 0 || frameSize.y === 0) {
        return defaultDocumentCrop;
    }

    return {
        unit: "%",
        width: (propertyBox.size.x * 100) / frameSize.x,
        height: (propertyBox.size.y * 100) / frameSize.y,
        x: (propertyBox.position.x * 100) / frameSize.x,
        y: (propertyBox.position.y * 100) / frameSize.y,
    };
}

const ScreenshotDialog: React.FC<ScreenshotPickerProps> = ({
    frame,
    propertyName,
    documentRelativePosition,
    documentSize,
    resources,
    onImageSelected,
    toggle,
    maxWidth,
    maxHeight,
}) => {
    const defaultCrop = useMemo(
        () => getDefaultCrop(propertyName, frame, documentRelativePosition, documentSize),
        [propertyName, frame, documentRelativePosition, documentSize],
    );

    const [crop, setCrop] = useState<Crop>(defaultCrop);
    const [input, setInput] = useState<string>(null);
    const [drawerInput, setDrawerInput] = useState<ResizeResult>(null);
    const imageRef = useRef<any>(null);

    const mode = useMemo(() => {
        if (input) {
            return Mode.Crop;
        }
        if (drawerInput) {
            return Mode.Highlight;
        }
        return Mode.Default;
    }, [input, drawerInput]);

    useEffect(() => {
        let isCurrent = true;
        frame
            .takeScreenshot()
            .then((image) => isCurrent && setInput(image))
            .catch((error) => {
                console.warn("Couldn't take a screenshot", error);
                if (isCurrent) {
                    toggle();
                }
            });

        return () => {
            isCurrent = false;
        };
    }, [frame]);

    const cancel = () => {
        setCrop(defaultCrop);
        setInput(null);
        setDrawerInput(null);
        toggle();
    };

    const cropImage = async () => {
        if (!crop) {
            return;
        }

        const croppedImg = getCroppedImg(imageRef.current);
        const resizedImage = await resize(croppedImg, maxWidth, maxHeight);
        setCrop(defaultCrop);
        setInput(null);
        setDrawerInput(resizedImage);
    };

    const toPixelCrop = (image: HTMLImageElement): Crop => {
        if (crop.unit !== "%") {
            return crop;
        }

        return {
            unit: "px",
            x: (crop.x * image.width) / 100,
            y: (crop.y * image.height) / 100,
            width: (crop.width * image.width) / 100,
            height: (crop.height * image.height) / 100,
        };
    };

    const getCroppedImg = (image) => {
        const pixelCrop = toPixelCrop(image);
        const canvas = document.createElement("canvas");
        const scaleX = image.naturalWidth / image.width;
        const scaleY = image.naturalHeight / image.height;
        canvas.width = pixelCrop.width;
        canvas.height = pixelCrop.height;
        const ctx = canvas.getContext("2d");

        ctx.drawImage(
            image,
            pixelCrop.x * scaleX,
            pixelCrop.y * scaleY,
            pixelCrop.width * scaleX,
            pixelCrop.height * scaleY,
            0,
            0,
            pixelCrop.width,
            pixelCrop.height,
        );

        return canvas.toDataURL();
    };

    const onImageLoaded = (image) => {
        imageRef.current = image;
    };

    const onCropComplete = (newCrop) => {
        setCrop(newCrop);
    };

    const remove = () => {
        onImageSelected(null);
        setCrop(defaultCrop);
        setInput(null);
        setDrawerInput(null);
    };

    const onCancel = () => {
        remove();
        toggle();
    };

    const onApplyDrawing = (img) => {
        onImageSelected(img);
        setCrop(defaultCrop);
        setInput(null);
        setDrawerInput(null);
        toggle();
    };

    return (
        <Dialog className="screenshot-picker-dialog" open={mode !== Mode.Default} onClose={cancel}>
            <DialogTitle>
                <div className="header">{resources.screenshot.cropandhighlight}</div>
            </DialogTitle>
            <DialogContent>
                <div className="screenshot-picker">
                    {mode === Mode.Crop && (
                        <>
                            <ReactCrop
                                className="screenshot-cropper"
                                crop={crop}
                                onImageLoaded={onImageLoaded}
                                src={input}
                                onChange={setCrop}
                                onComplete={onCropComplete}
                            />
                            <DialogActions>
                                <Button onClick={cancel}>cancel</Button>
                                <Button onClick={cropImage} disabled={!crop.width}>
                                    Crop
                                </Button>
                            </DialogActions>
                        </>
                    )}
                    {mode === Mode.Highlight && (
                        <DrawablePreview
                            src={drawerInput.image}
                            width={drawerInput.width}
                            height={drawerInput.height}
                            onCancel={onCancel}
                            onApplyDrawing={onApplyDrawing}
                        />
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
};

export default inject("resources")(observer(ScreenshotDialog));
