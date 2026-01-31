import { getCssSelector } from "css-selector-generator";
import { inject, observer } from "mobx-react";
import React, { CSSProperties, useCallback, useEffect, useRef, useState } from "react";

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

const IframeOverlay: React.FC<IframeOverlayProps> = ({
    iframe,
    reviewStore,
    external,
    children,
    reviewLocationCreated,
}) => {
    const [offsetHeight, setOffsetHeight] = useState(0);
    const overlayRef = useRef<HTMLDivElement>(null);
    const overlayDocumentRef = useRef<HTMLDivElement>(null);

    const toggleOverlays = useCallback((show: boolean) => {
        if (overlayRef.current) {
            overlayRef.current.style.display = show ? "block" : "none";
        }
        const propertyOverlays = document.getElementsByClassName("epi-overlay-container");

        [].forEach.call(propertyOverlays, (overlay) => {
            overlay.style.display = show ? "block" : "none";
        });
    }, []);

    const scroll = useCallback(
        (e) => {
            if (overlayRef.current !== e.target) {
                return;
            }

            const scrollLeft = e.srcElement.scrollLeft;
            const scrollTop = e.srcElement.scrollTop;

            const parentContainer = iframe.parentNode as Element;
            if (parentContainer.nodeName === "BODY") {
                iframe.contentWindow.scrollTo(scrollLeft, scrollTop);
                return;
            }

            parentContainer.scrollLeft = scrollLeft;
            parentContainer.scrollTop = scrollTop;
        },
        [iframe],
    );

    const addReviewLocation = useCallback(
        (e) => {
            //TODO: why we have to hack like this? preventDefault should take care of this
            if (overlayDocumentRef.current !== e.target) {
                return;
            }

            let point: { x: number; y: number };
            if (external) {
                point = { x: e.pageX, y: e.pageY };
            } else {
                point = { x: e.offsetX, y: e.offsetY };
            }
            toggleOverlays(false);
            const clickedElement = iframe.contentDocument.elementFromPoint(point.x, point.y) as HTMLElement;
            const selector = getCssSelector(clickedElement);
            toggleOverlays(true);
            const nodeOffset = offset(clickedElement, external);

            const reviewLocation = new PinLocation(reviewStore, {
                documentRelativePosition: {
                    x: e.offsetX,
                    y: e.offsetY,
                },
                documentSize: {
                    x: overlayDocumentRef.current.offsetWidth,
                    y: overlayDocumentRef.current.offsetHeight,
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
                reviewLocation.propertyName =
                    propertyElement.dataset.epiPropertyName || propertyElement.dataset.epiEdit;
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
            reviewLocationCreated(reviewLocation);
        },
        [external, iframe, reviewStore, reviewLocationCreated, toggleOverlays],
    );

    useEffect(() => {
        const checkTime = 1000;
        const calculatePositionInterval = window.setInterval(() => {
            //TODO: when changing context very fast, sometimes we get into here before the component is unmounted, maybe can be solved differently?
            if (!iframe.contentDocument || !iframe.contentDocument.body) {
                return;
            }

            const newOffsetHeight = iframe.contentDocument.body.offsetHeight;
            if (offsetHeight !== newOffsetHeight) {
                setOffsetHeight(newOffsetHeight);
            }
        }, checkTime);

        if (overlayRef.current) {
            overlayRef.current.addEventListener("scroll", scroll, true);
            overlayRef.current.addEventListener("click", addReviewLocation);
        }

        return () => {
            clearInterval(calculatePositionInterval);
            if (overlayRef.current) {
                overlayRef.current.removeEventListener("scroll", scroll, true);
                overlayRef.current.removeEventListener("click", addReviewLocation);
            }
        };
    }, [iframe, offsetHeight, scroll, addReviewLocation]);

    const { height, top, width } = iframe.parentElement.style;

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
        height: offsetHeight,
    };

    return (
        <div id="review-overlay" ref={overlayRef} style={styles}>
            <div ref={overlayDocumentRef} style={documentStyles}>
                {children}
            </div>
        </div>
    );
};

export default inject("reviewStore")(observer(IframeOverlay));
