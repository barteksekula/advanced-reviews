import { Dimensions, PinPositioningDetails } from "../store/review-store";
import offset from "./offset";

interface PositionCalculator {
    calculate: (location: PinPositioningDetails) => Dimensions;
}

export default function createPositionCalculator(
    documentSize: Dimensions,
    external: boolean,
    document?: Document,
): PositionCalculator {
    const positionDomNode = (
        node: HTMLElement,
        documentRelativeOffset: Dimensions,
        nodePosition: Dimensions,
        nodeSize: Dimensions,
    ): Dimensions => {
        const nodeOffset = offset(node, external);

        const originalOffsetFromLeft = documentRelativeOffset.x - nodePosition.x;
        const originalOffsetFromTop = documentRelativeOffset.y - nodePosition.y;

        const currentOffsetFromLeft = nodeOffset.left;
        const currentOffsetFromTop = nodeOffset.top;

        const xPropertyFactor = node.offsetWidth / nodeSize.x;
        const yPropertyFactor = node.offsetHeight / nodeSize.y;

        return {
            x: currentOffsetFromLeft + originalOffsetFromLeft * xPropertyFactor,
            y: currentOffsetFromTop + originalOffsetFromTop * yPropertyFactor,
        };
    };

    const resize = (location: PinPositioningDetails): Dimensions => {
        const xFactor = location.documentRelativePosition.x / location.documentSize.x;
        const yFactor = location.documentRelativePosition.y / location.documentSize.y;

        return {
            x: xFactor * documentSize.x,
            y: yFactor * documentSize.y,
        };
    };

    const calculate = (location: PinPositioningDetails): Dimensions => {
        if (document) {
            if (location.clickedDomNodeSelector && location.clickedDomNodePosition && location.clickedDomNodeSize) {
                const node: HTMLElement = document.querySelector(location.clickedDomNodeSelector);
                if (!node) {
                    return resize(location);
                }

                return positionDomNode(
                    node,
                    location.documentRelativePosition,
                    location.clickedDomNodePosition,
                    location.clickedDomNodeSize,
                );
            } else if (location.propertyName && location.propertyPosition && location.propertySize) {
                const node: HTMLElement = document.querySelector(`[data-epi-property-name='${location.propertyName}']`);
                if (!node) {
                    return resize(location);
                }
                return positionDomNode(
                    node,
                    location.documentRelativePosition,
                    location.propertyPosition,
                    location.propertySize,
                );
            }
        }

        return resize(location);
    };

    return {
        calculate,
    };
}
