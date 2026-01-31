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
            positionCalculator = createPositionCalculator(originalDocumentLocation, false);
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
            positionCalculator = createPositionCalculator(newDocumentSize, false);
        });

        test("it should return rescaled position relative to the new document size", () => {
            const dimensions = positionCalculator.calculate(pin);
            const expectedPinLocation: Dimensions = { x: 150, y: 150 };
            expect(dimensions).toStrictEqual(expectedPinLocation);
        });
    });
});

//TODO: add more tests with faked HTML document

// describe("when property position data is available", () => {
//     let pin: PinPositioningDetails;
//     let positionCalculator: ReturnType<typeof createPositionCalculator>;
//
//     beforeEach(() => {
//         pin = getPin({
//             propertyName: "myproperty",
//             propertyPosition: { x: 1, y: 1 },
//             propertySize: { x: 1, y: 1 }
//         });
//         positionCalculator = createPositionCalculator(originalDocumentLocation, false);
//     });
//
//     test("it should return position relative to the document", () => {
//         const dimensions = positionCalculator.calculate(pin);
//         expect(dimensions).toBe(originalPinLocation);
//     });
// });
