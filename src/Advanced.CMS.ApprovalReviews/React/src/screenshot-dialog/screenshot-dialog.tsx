import "./screenshot-dialog.scss";

import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import html2canvas from "html2canvas";
import { computed } from "mobx";
import { inject, observer } from "mobx-react";
import React from "react";
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

interface ScreenshotPickerState {
    crop: Crop;
    input: string;
    drawerInput: ResizeResult;
}

enum Mode {
    Default,
    Crop,
    Highlight,
}

interface ResizeResult {
    image: string;
    width: number;
    height: number;
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

@inject("resources")
@observer
export default class ScreenshotDialog extends React.Component<ScreenshotPickerProps, ScreenshotPickerState> {
    defaultCrop = {
        width: 50,
        height: 50,
        x: 10,
        y: 10,
    };
    private imageRef: any;

    constructor(props) {
        super(props);
        this.state = {
            crop: this.getDefaultCrop(),
            input: null,
            drawerInput: null,
        };
    }

    @computed get mode() {
        if (this.state.input) {
            return Mode.Crop;
        }
        if (this.state.drawerInput) {
            return Mode.Highlight;
        }

        return Mode.Default;
    }

    onCropChange = (crop) => {
        this.setState({ crop });
    };

    componentDidMount(): void {
        html2canvas(this.props.iframe.contentDocument.body, {
            allowTaint: true,
            useCORS: true,
        }).then((canvas) => {
            this.setState({ input: canvas.toDataURL() });
        });
    }

    cancel = () => {
        this.setState({ crop: this.defaultCrop, input: null, drawerInput: null });
        this.props.toggle();
    };

    crop = async () => {
        if (!this.state.crop) {
            return;
        }

        const croppedImg = this.getCroppedImg(this.imageRef);
        const resizedImage = await resize(croppedImg, this.props.maxWidth, this.props.maxHeight);
        this.setState({ crop: this.defaultCrop, input: null, drawerInput: resizedImage });
    };

    getCroppedImg(image) {
        const crop = this.state.crop;
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
    }

    onImageLoaded = (image) => {
        this.imageRef = image;
    };

    onCropComplete = (crop) => {
        this.setState({ crop });
    };

    remove = () => {
        this.props.onImageSelected(null);
        this.setState({ crop: this.defaultCrop, input: null, drawerInput: null });
    };

    onCancel = () => {
        this.remove();
        this.props.toggle();
    };

    onApplyDrawing = (img) => {
        this.props.onImageSelected(img);
        this.setState({ crop: this.defaultCrop, input: null, drawerInput: null });
        this.props.toggle();
    };

    getDefaultCrop = () => {
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
            getDefaultDocumentCrop(this.props.documentRelativePosition, this.props.documentSize) ||
            Object.assign(this.defaultCrop);

        if (!this.props.propertyName) {
            return defaultDocumentCrop;
        }

        const propertyEl: HTMLElement = this.props.iframe.contentDocument.querySelector(
            `[data-epi-property-name='${this.props.propertyName}']`,
        );
        if (!propertyEl) {
            return defaultDocumentCrop;
        }

        const iframeWidth = this.props.iframe.offsetWidth;
        const iframeHeight = this.props.iframe.offsetHeight;

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
    };

    render() {
        return (
            <Dialog className="screenshot-picker-dialog" open={this.mode !== Mode.Default} onClose={this.cancel}>
                <DialogTitle>
                    <div className="header">{this.props.resources.screenshot.cropandhighlight}</div>
                </DialogTitle>
                <DialogContent>
                    <div className="screenshot-picker">
                        {this.mode === Mode.Crop && (
                            <>
                                <ReactCrop
                                    className="screenshot-cropper"
                                    crop={this.state.crop}
                                    onImageLoaded={this.onImageLoaded}
                                    src={this.state.input}
                                    onChange={this.onCropChange}
                                    onComplete={this.onCropComplete}
                                />
                                <DialogActions>
                                    <Button onClick={this.cancel}>cancel</Button>
                                    <Button onClick={this.crop} disabled={!this.state.crop.width}>
                                        Crop
                                    </Button>
                                </DialogActions>
                            </>
                        )}
                        {this.mode === Mode.Highlight && (
                            <DrawablePreview
                                src={this.state.drawerInput.image}
                                width={this.state.drawerInput.width}
                                height={this.state.drawerInput.height}
                                onCancel={this.onCancel}
                                onApplyDrawing={this.onApplyDrawing}
                            />
                        )}
                    </div>
                </DialogContent>
            </Dialog>
        );
    }
}
