import "./screenshot-dialog.scss";

import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import html2canvas from "html2canvas";
import { inject, observer } from "mobx-react";
import React, { useEffect, useMemo, useRef, useState } from "react";
import ReactCrop, { Crop } from "react-image-crop";

import DrawablePreview from "../drawable-preview/drawable-preview";
import { Dimensions } from "../store/review-store";

interface ScreenshotPickerProps {
    iframe: HTMLIFrameElement;
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
    iframe: HTMLIFrameElement,
    documentRelativePosition: Dimensions,
    documentSize: Dimensions,
) {
    const defaultCrop = {
        width: 50,
        height: 50,
        x: 10,
        y: 10,
    };

    function offset(el: HTMLElement) {
        const rect = el.getBoundingClientRect(),
            scrollLeft = window.pageXOffset || document.documentElement.scrollLeft,
            scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        return { top: rect.top + scrollTop, left: rect.left + scrollLeft };
    }

    // if user didn't click on the property then try to show default crop on the pin location
    function getDefaultDocumentCrop(position: Dimensions, size: Dimensions) {
        const rectangleSize = 20;

        if (!position || !size) {
            return null;
        }

        if (size.x === 0 || size.y === 0) {
            return null;
        }

        const defaultDocumentCrop: any = {};
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

    const defaultDocumentCrop =
        getDefaultDocumentCrop(documentRelativePosition, documentSize) || Object.assign(defaultCrop);

    if (!propertyName) {
        return defaultDocumentCrop;
    }

    const propertyEl: HTMLElement = iframe.contentDocument.querySelector(`[data-epi-property-name='${propertyName}']`);
    if (!propertyEl) {
        return defaultDocumentCrop;
    }

    const iframeWidth = iframe.offsetWidth;
    const iframeHeight = iframe.offsetHeight;

    if (iframeWidth === 0 || iframeHeight === 0) {
        return defaultDocumentCrop;
    }

    const elWidth = propertyEl.offsetWidth;
    const elHeight = propertyEl.offsetHeight;

    const percentageWidth = (elWidth * 100) / iframeWidth;
    const percentageHeight = (elHeight * 100) / iframeHeight;

    const elOffset = offset(propertyEl);
    const percentX = (elOffset.left * 100) / iframeWidth;
    const percentY = (elOffset.top * 100) / iframeHeight;

    return {
        width: percentageWidth,
        height: percentageHeight,
        x: percentX,
        y: percentY,
    };
}

const ScreenshotDialog: React.FC<ScreenshotPickerProps> = ({
    iframe,
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
        () => getDefaultCrop(propertyName, iframe, documentRelativePosition, documentSize),
        [propertyName, iframe, documentRelativePosition, documentSize],
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
        html2canvas(iframe.contentDocument.body, {
            allowTaint: true,
            useCORS: true,
        }).then((canvas) => {
            setInput(canvas.toDataURL());
        });
    }, [iframe]);

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

    const getCroppedImg = (image) => {
        const canvas = document.createElement("canvas");
        const scaleX = image.naturalWidth / image.width;
        const scaleY = image.naturalHeight / image.height;
        canvas.width = crop.width;
        canvas.height = crop.height;
        const ctx = canvas.getContext("2d");

        ctx.drawImage(
            image,
            crop.x * scaleX,
            crop.y * scaleY,
            crop.width * scaleX,
            crop.height * scaleY,
            0,
            0,
            crop.width,
            crop.height,
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
