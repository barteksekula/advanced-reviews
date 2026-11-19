import type { ElementBox, PreviewFrame } from "../preview-frame/preview-frame";
import { Dimensions, PinPositioningDetails } from "../store/review-store";

interface PositionCalculator {
    calculate: (location: PinPositioningDetails) => Dimensions;
}

export default function createPositionCalculator(
    documentSize: Dimensions,
    frame?: Pick<PreviewFrame, "getNodeBox" | "getPropertyBox">,
): PositionCalculator {
    const positionInBox = (
        box: ElementBox,
        documentRelativeOffset: Dimensions,
        nodePosition: Dimensions,
        nodeSize: Dimensions,
    ): Dimensions => {
        const originalOffsetFromLeft = documentRelativeOffset.x - nodePosition.x;
        const originalOffsetFromTop = documentRelativeOffset.y - nodePosition.y;

        const xPropertyFactor = box.size.x / nodeSize.x;
        const yPropertyFactor = box.size.y / nodeSize.y;

        return {
            x: box.position.x + originalOffsetFromLeft * xPropertyFactor,
            y: box.position.y + originalOffsetFromTop * yPropertyFactor,
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
        if (!frame) {
            return resize(location);
        }

        if (location.clickedDomNodeSelector && location.clickedDomNodePosition && location.clickedDomNodeSize) {
            const box = frame.getNodeBox(location.clickedDomNodeSelector);
            return box
                ? positionInBox(
                      box,
                      location.documentRelativePosition,
                      location.clickedDomNodePosition,
                      location.clickedDomNodeSize,
                  )
                : resize(location);
        }

        if (location.propertyName && location.propertyPosition && location.propertySize) {
            const box = frame.getPropertyBox(location.propertyName);
            return box
                ? positionInBox(
                      box,
                      location.documentRelativePosition,
                      location.propertyPosition,
                      location.propertySize,
                  )
                : resize(location);
        }

        return resize(location);
    };

    return {
        calculate,
    };
}
