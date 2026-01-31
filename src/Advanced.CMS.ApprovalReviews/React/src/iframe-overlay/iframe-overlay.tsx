import { getCssSelector } from "css-selector-generator";
import { inject, observer } from "mobx-react";
import React, { CSSProperties } from "react";

import offset from "../position-calculator/offset";
import { IReviewComponentStore, PinLocation } from "../store/review-store";

interface IframeOverlayProps {
    iframe: HTMLIFrameElement;
    reviewStore?: IReviewComponentStore;
    external: boolean;
    children?: React.ReactNode;

    reviewLocationCreated(location: PinLocation): void;
}

const getClosest = (element, selector): HTMLElement => {
    for (; element && element !== document; element = element.parentNode) {
        if (typeof element.matches === "function" && element.matches(selector)) return element;
    }
    return null;
};

class IframeOverlay extends React.Component<IframeOverlayProps, any> {
    calculatePositionInterval: number;
    private overlayRef: React.RefObject<HTMLDivElement>;
    private overlayDocumentRef: React.RefObject<HTMLDivElement>;

    constructor(props: IframeOverlayProps) {
        super(props);
        this.overlayRef = React.createRef<HTMLDivElement>();
        this.overlayDocumentRef = React.createRef<HTMLDivElement>();
        this.state = {
            offsetHeight: 0,
        };
    }

    componentDidMount() {
        const checkTime = 1000;
        this.calculatePositionInterval = window.setInterval(
            function () {
                //TODO: when changing context very fast, sometimes we get into here before the component is unmounted, maybe can be solved differently?
                if (!this.props.iframe.contentDocument || !this.props.iframe.contentDocument.body) {
                    return;
                }

                const { offsetHeight } = this.props.iframe.contentDocument.body;
                if (this.state.offsetHeight === offsetHeight) {
                    return;
                }

                this.setState({
                    offsetHeight: offsetHeight,
                });
            }.bind(this),
            checkTime,
        );
        this.overlayRef.current.addEventListener("scroll", this.scroll.bind(this), true);
        this.overlayRef.current.addEventListener("click", this.addReviewLocation.bind(this));
    }

    componentWillUnmount() {
        clearInterval(this.calculatePositionInterval);
        this.overlayRef.current.removeEventListener("scroll", this.scroll.bind(this), true);
        this.overlayRef.current.removeEventListener("click", this.addReviewLocation.bind(this));
    }

    scroll(e) {
        if (this.overlayRef.current !== e.target) {
            return;
        }

        const scrollLeft = e.srcElement.scrollLeft;
        const scrollTop = e.srcElement.scrollTop;

        const parentContainer = this.props.iframe.parentNode as Element;
        if (parentContainer.nodeName === "BODY") {
            this.props.iframe.contentWindow.scrollTo(scrollLeft, scrollTop);
            return;
        }

        parentContainer.scrollLeft = scrollLeft;
        parentContainer.scrollTop = scrollTop;
    }

    toggleOverlays(show) {
        this.overlayRef.current.style.display = show ? "block" : "none";
        const propertyOverlays = document.getElementsByClassName("epi-overlay-container");

        [].forEach.call(propertyOverlays, (overlay) => {
            overlay.style.display = show ? "block" : "none";
        });
    }

    addReviewLocation(e) {
        //TODO: why we have to hack like this? preventDefault should take care of this
        if (this.overlayDocumentRef.current !== e.target) {
            return;
        }

        let point: { x: number; y: number };
        if (this.props.external) {
            point = { x: e.pageX, y: e.pageY };
        } else {
            point = { x: e.offsetX, y: e.offsetY };
        }
        this.toggleOverlays(false);
        const clickedElement = this.props.iframe.contentDocument.elementFromPoint(point.x, point.y) as HTMLElement;
        const selector = getCssSelector(clickedElement);
        this.toggleOverlays(true);
        const nodeOffset = offset(clickedElement, this.props.external);

        const reviewLocation = new PinLocation(this.props.reviewStore, {
            documentRelativePosition: {
                x: e.offsetX,
                y: e.offsetY,
            },
            documentSize: {
                x: this.overlayDocumentRef.current.offsetWidth,
                y: this.overlayDocumentRef.current.offsetHeight,
            },
            isDone: false,
            clickedDomNodeSelector: selector,
            clickedDomNodeSize: {
                x: clickedElement.offsetWidth,
                y: clickedElement.offsetHeight,
            },
            clickedDomNodePosition: {
                x: nodeOffset.left,
                y: nodeOffset.top,
            },
        });

        const propertyElement =
            getClosest(clickedElement, "[data-epi-property-name]") || getClosest(clickedElement, "[data-epi-edit]");
        if (propertyElement) {
            // if property is found we want to remember its offsets as well
            reviewLocation.propertyName = propertyElement.dataset.epiPropertyName || propertyElement.dataset.epiEdit;
            reviewLocation.propertyPosition = { x: propertyElement.offsetLeft, y: propertyElement.offsetTop };
            reviewLocation.propertySize = { x: propertyElement.offsetWidth, y: propertyElement.offsetHeight };

            const blockElement = getClosest(clickedElement, "[data-epi-block-id]");
            if (blockElement) {
                reviewLocation.blockId = blockElement.dataset.epiBlockId;
                reviewLocation.blockName = blockElement.dataset.epiContentName;
                reviewLocation.blockPosition = { x: blockElement.offsetLeft, y: blockElement.offsetTop };
                reviewLocation.blockSize = { x: blockElement.offsetWidth, y: blockElement.offsetHeight };
            }
        }
        this.props.reviewLocationCreated(reviewLocation);
    }

    render() {
        const { height, top, width } = this.props.iframe.parentElement.style;

        const styles: CSSProperties = {
            position: "absolute",
            zIndex: 600,
            overflowY: "auto",
            top: top,
            height: height,
            maxHeight: height,
            width: width,
            cursor: "crosshair",
        };

        const documentStyles: CSSProperties = {
            height: this.state.offsetHeight,
        };

        return (
            <div id="review-overlay" ref={this.overlayRef} style={styles}>
                <div ref={this.overlayDocumentRef} style={documentStyles}>
                    {this.props.children}
                </div>
            </div>
        );
    }
}

export default inject("reviewStore")(observer(IframeOverlay));
