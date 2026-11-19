import { inject, observer } from "mobx-react";
import React, { CSSProperties, useCallback, useEffect, useRef, useState } from "react";

import { PreviewFrame } from "../preview-frame/preview-frame";
import { IReviewComponentStore, PinLocation } from "../store/review-store";

interface IframeOverlayProps {
    frame: PreviewFrame;
    reviewStore?: IReviewComponentStore;
    external: boolean;
    children?: React.ReactNode;

    reviewLocationCreated(location: PinLocation): void;
}

interface OverlayLayout {
    top: number;
    left: number;
    width: number;
    height: number;
}

const IframeOverlay: React.FC<IframeOverlayProps> = ({
    frame,
    reviewStore,
    external,
    children,
    reviewLocationCreated,
}) => {
    const [offsetHeight, setOffsetHeight] = useState(() => frame.getDocumentSize().y);
    const overlayRef = useRef<HTMLDivElement>(null);
    const overlayDocumentRef = useRef<HTMLDivElement>(null);
    const [layout, setLayout] = useState<OverlayLayout>(null);

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

            frame.scroll(e.target.scrollLeft, e.target.scrollTop);
        },
        [frame],
    );

    const addReviewLocation = useCallback(
        (e) => {
            //TODO: why we have to hack like this? preventDefault should take care of this
            if (overlayDocumentRef.current !== e.target) {
                return;
            }

            const documentPoint = { x: e.offsetX, y: e.offsetY };
            toggleOverlays(false);
            const hit = frame.hitTest(documentPoint);
            toggleOverlays(true);

            const reviewLocation = new PinLocation(reviewStore, {
                documentRelativePosition: documentPoint,
                documentSize: {
                    x: overlayDocumentRef.current.offsetWidth,
                    y: overlayDocumentRef.current.offsetHeight,
                },
                isDone: false,
                clickedDomNodeSelector: hit.node?.selector,
                clickedDomNodeSize: hit.node?.size,
                clickedDomNodePosition: hit.node?.position,
            });

            if (hit.property) {
                reviewLocation.propertyName = hit.property.name;
                reviewLocation.propertyPosition = hit.property.position;
                reviewLocation.propertySize = hit.property.size;
            }

            if (hit.property && hit.block) {
                reviewLocation.blockId = hit.block.id;
                reviewLocation.blockName = hit.block.name;
                reviewLocation.blockPosition = hit.block.position;
                reviewLocation.blockSize = hit.block.size;
            }

            reviewLocationCreated(reviewLocation);
        },
        [frame, reviewStore, reviewLocationCreated, toggleOverlays],
    );

    const updateLayout = useCallback(() => {
        const overlay = overlayRef.current;
        if (external || !overlay) {
            return;
        }

        const container = frame.getScrollContainer();
        const host = (overlay.offsetParent as HTMLElement) ?? overlay.parentElement;
        const containerRect = container.getBoundingClientRect();
        const hostRect = host.getBoundingClientRect();
        const next = {
            top: containerRect.top - hostRect.top + host.scrollTop,
            left: containerRect.left - hostRect.left + host.scrollLeft,
            width: container.clientWidth,
            height: container.clientHeight,
        };
        setLayout((current) =>
            current &&
            current.top === next.top &&
            current.left === next.left &&
            current.width === next.width &&
            current.height === next.height
                ? current
                : next,
        );
        const scrollTop = frame.getScrollPosition().y;
        if (overlay.scrollTop !== scrollTop) {
            overlay.scrollTop = scrollTop;
        }
    }, [external, frame]);

    useEffect(() => {
        const update = () => {
            const size = frame.getDocumentSize().y;
            setOffsetHeight(external ? size : Math.max(size, frame.iframe.offsetHeight));
            updateLayout();
        };
        const unsubscribe = frame.subscribe(update);
        const calculatePositionInterval = window.setInterval(update, 1000);
        update();

        const overlay = overlayRef.current;
        overlay?.addEventListener("scroll", scroll, true);
        overlay?.addEventListener("click", addReviewLocation);
        window.addEventListener("resize", updateLayout);

        return () => {
            unsubscribe();
            clearInterval(calculatePositionInterval);
            overlay?.removeEventListener("scroll", scroll, true);
            overlay?.removeEventListener("click", addReviewLocation);
            window.removeEventListener("resize", updateLayout);
        };
    }, [external, frame, scroll, addReviewLocation, updateLayout]);

    const { height, top } = frame.iframe.parentElement.style;

    const styles: CSSProperties = {
        position: "absolute",
        zIndex: 600,
        overflowY: "auto",
        cursor: "crosshair",
        ...(external || !layout
            ? { top: top, height: height, maxHeight: height, width: "100%" }
            : { ...layout, maxHeight: layout.height }),
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
