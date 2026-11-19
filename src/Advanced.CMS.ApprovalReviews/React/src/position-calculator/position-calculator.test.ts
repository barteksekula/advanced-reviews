import { beforeEach, describe, expect, test } from "vitest";

import { Dimensions, PinPositioningDetails } from "../store/review-store";
import createPositionCalculator from "./position-calculator";

const originalPinLocation: Dimensions = { x: 100, y: 200 };
const originalDocumentLocation: Dimensions = { x: 800, y: 800 };

const getPin = (extraProps?): PinPositioningDetails => {
    const pin = {
        documentRelativePosition: originalPinLocation,
        documentSize: originalDocumentLocation,
    };
    Object.assign(pin, extraProps || {});
    return pin;
};

describe("when property position data is not available", () => {
    let pin: PinPositioningDetails;
    let positionCalculator: ReturnType<typeof createPositionCalculator>;

    describe("and the document size is the same as the saved one", () => {
        beforeEach(() => {
            pin = getPin();
            positionCalculator = createPositionCalculator(originalDocumentLocation);
        });

        test("it should return original position", () => {
            const dimensions = positionCalculator.calculate(pin);
            expect(dimensions).toStrictEqual(originalPinLocation);
        });
    });

    describe("and the document size is different then the saved one", () => {
        beforeEach(() => {
            pin = getPin();
            const newDocumentSize: Dimensions = { x: 1200, y: 600 };
            positionCalculator = createPositionCalculator(newDocumentSize);
        });

        test("it should return rescaled position relative to the new document size", () => {
            const dimensions = positionCalculator.calculate(pin);
            const expectedPinLocation: Dimensions = { x: 150, y: 150 };
            expect(dimensions).toStrictEqual(expectedPinLocation);
        });
    });
});

describe("when property position data is available", () => {
    const pin = getPin({
        propertyName: "MainBody",
        propertyPosition: { x: 50, y: 150 },
        propertySize: { x: 200, y: 100 },
    });

    test("it should keep the pin at the same relative place in the property when the property moves", () => {
        const positionCalculator = createPositionCalculator(originalDocumentLocation, {
            getNodeBox: () => null,
            getPropertyBox: () => ({ position: { x: 60, y: 300 }, size: { x: 400, y: 100 } }),
        });

        expect(positionCalculator.calculate(pin)).toStrictEqual({ x: 160, y: 350 });
    });

    test("it should rescale the position when the property is not found", () => {
        const positionCalculator = createPositionCalculator(
            { x: 1600, y: 800 },
            { getNodeBox: () => null, getPropertyBox: () => null },
        );

        expect(positionCalculator.calculate(pin)).toStrictEqual({ x: 200, y: 200 });
    });
});
