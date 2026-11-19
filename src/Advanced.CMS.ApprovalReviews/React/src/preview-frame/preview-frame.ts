import { getCssSelector } from "css-selector-generator";
import html2canvas from "html2canvas";

import offset from "../position-calculator/offset";
import { Dimensions } from "../store/review-store";

export interface ElementBox {
    position: Dimensions;
    size: Dimensions;
}

export interface FrameHit {
    node?: ElementBox & { selector: string };
    property?: ElementBox & { name: string };
    block?: ElementBox & { id: string; name?: string };
}

export interface PreviewFrame {
    readonly iframe: HTMLIFrameElement;
    isRemote(): boolean;
    getDisplayNames(): Record<string, string>;
    getDocumentSize(): Dimensions;
    getNodeBox(selector: string): ElementBox | null;
    getPropertyBox(name: string): ElementBox | null;
    hitTest(framePoint: Dimensions, documentPoint: Dimensions): FrameHit;
    getScrollContainer(): HTMLElement;
    scroll(left: number, top: number): void;
    takeScreenshot(): Promise<string>;
    subscribe(listener: () => void): () => void;
    dispose(): void;
}

interface RemoteNode {
    offsetLeft: number;
    offsetTop: number;
    offsetWidth: number;
    offsetHeight: number;
}

interface RemoteProperty extends RemoteNode {
    name: string;
    displayName?: string;
    parentBlockId?: string;
}

interface RemotePropertyMessage extends RemoteNode {
    name: string;
    parentBlockId?: string;
    dataset?: { epiPropertyName?: string; epiEdit?: string };
}

interface RemoteBlock extends RemoteNode {
    id: string;
    blockNodes?: RemoteBlock[];
}

interface RemoteState {
    size?: Dimensions;
    properties: RemoteProperty[];
    blocks: RemoteBlock[];
}

const messages = {
    load: "/site/load",
    unload: "/site/unload",
    resize: "/site/resize",
    properties: "/site/properties",
    blocks: "/site/blocks",
    checkSize: "/site/checksize",
    screenshotRequest: "/site/screenshot:cms",
    screenshotResponse: "/site/screenshot:delivery",
};

const screenshotTimeout = 20000;
const checkSizeInterval = 1000;
const layoutRequestDelay = 50;
const scrollableOverflow = /^(auto|scroll|overlay)$/;

const remoteStates = new WeakMap<object, RemoteState>();
const stateListeners = new Set<(source: object) => void>();
const screenshotRequests = new Map<string, { resolve: (image: string) => void; reject: (error: Error) => void }>();

const emptyState = (): RemoteState => ({ properties: [], blocks: [] });

const getState = (source: object): RemoteState => {
    let state = remoteStates.get(source);
    if (!state) {
        state = emptyState();
        remoteStates.set(source, state);
    }
    return state;
};

const matches = (id: string, message: string) => id === message || id.endsWith(message);

const toNode = ({ offsetLeft, offsetTop, offsetWidth, offsetHeight }: RemoteNode): RemoteNode => ({
    offsetLeft,
    offsetTop,
    offsetWidth,
    offsetHeight,
});

const toProperty = (property: RemotePropertyMessage): RemoteProperty => {
    const name = property.dataset?.epiPropertyName || property.dataset?.epiEdit || property.name;
    return {
        ...toNode(property),
        name,
        displayName: property.name && property.name !== name ? property.name : undefined,
        parentBlockId: property.parentBlockId,
    };
};

const toProperties = (properties: RemotePropertyMessage[] | undefined): RemoteProperty[] | undefined =>
    Array.isArray(properties) ? properties.map(toProperty) : undefined;

const flattenBlocks = (blocks: RemoteBlock[] = []): RemoteBlock[] =>
    blocks.flatMap((block) => [{ ...toNode(block), id: block.id }, ...flattenBlocks(block.blockNodes)]);

const onMessage = (event: MessageEvent) => {
    const id = event.data?.id;
    const data = event.data?.data;
    if (typeof id !== "string" || !event.source || !id.includes("/site/")) {
        return;
    }

    if (matches(id, messages.screenshotResponse)) {
        const request = screenshotRequests.get(data?.eventId);
        if (request) {
            screenshotRequests.delete(data.eventId);
            if (data.thumbnailDataUrl) {
                request.resolve(data.thumbnailDataUrl);
            } else {
                request.reject(new Error("The website could not create a screenshot"));
            }
        }
        return;
    }

    const state = getState(event.source);
    if (matches(id, messages.load) || matches(id, messages.unload)) {
        remoteStates.set(event.source, emptyState());
    } else if (matches(id, messages.resize)) {
        state.size = { x: data?.w, y: data?.h };
        state.properties = toProperties(data?.properties) ?? state.properties;
    } else if (matches(id, messages.properties)) {
        state.properties = toProperties(data?.properties) ?? [];
    } else if (matches(id, messages.blocks)) {
        state.blocks = flattenBlocks(data?.blockNodes);
    } else {
        return;
    }

    stateListeners.forEach((listener) => listener(event.source));
};

if (typeof window !== "undefined") {
    window.addEventListener("message", onMessage);
}

const toBox = (node: RemoteNode): ElementBox => ({
    position: { x: node.offsetLeft, y: node.offsetTop },
    size: { x: node.offsetWidth, y: node.offsetHeight },
});

const contains = (node: RemoteNode, point: Dimensions) =>
    point.x >= node.offsetLeft &&
    point.x <= node.offsetLeft + node.offsetWidth &&
    point.y >= node.offsetTop &&
    point.y <= node.offsetTop + node.offsetHeight;

const smallestContaining = <T extends RemoteNode>(nodes: T[], point: Dimensions): T | undefined =>
    nodes
        .filter((node) => contains(node, point))
        .sort((a, b) => a.offsetWidth * a.offsetHeight - b.offsetWidth * b.offsetHeight)[0];

const getClosest = (element: Element, selector: string): HTMLElement => element?.closest<HTMLElement>(selector) ?? null;

const propertySelector = (name: string) =>
    `[data-epi-property-name='${CSS.escape(name)}'], [data-epi-edit='${CSS.escape(name)}']`;

function createRandomId() {
    return `${Date.now()}-${Math.random().toString(36).substring(2)}`;
}

/**
 * Creates a frame that reads the previewed page through the DOM when it is served from the same origin,
 * and through the messages sent by the CMS communication injector when it is served by a remote website
 */
export default function createPreviewFrame(iframe: HTMLIFrameElement, external = false): PreviewFrame {
    const listeners = new Set<() => void>();
    const notify = () => listeners.forEach((listener) => listener());

    const getDocument = (): Document | null => {
        try {
            const document = iframe.contentDocument;
            return document?.body ? document : null;
        } catch {
            return null;
        }
    };

    const isRemote = () => !getDocument();

    const getRemoteState = (): RemoteState | null => {
        const source = iframe.contentWindow;
        return source ? getState(source) : null;
    };

    const requestSize = () => {
        if (isRemote()) {
            iframe.contentWindow?.postMessage({ id: messages.checkSize }, "*");
        }
    };

    const fitRemoteFrame = () => {
        const size = getRemoteState()?.size;
        if (external && isRemote() && size?.y) {
            iframe.style.height = `${size.y}px`;
        }
    };

    const onStateChanged = (source: object) => {
        if (source !== iframe.contentWindow) {
            return;
        }
        fitRemoteFrame();
        notify();
    };

    const onLoad = () => {
        try {
            iframe.contentWindow?.addEventListener("resize", notify);
        } catch {
            requestSize();
        }
        notify();
    };

    const requestLayout = () => {
        if (!isRemote() || getRemoteState()?.properties.length) {
            return;
        }

        const width = iframe.style.width;
        iframe.style.width = `${iframe.offsetWidth - 1}px`;
        window.setTimeout(() => {
            requestSize();
            iframe.style.width = width;
        }, layoutRequestDelay);
    };

    stateListeners.add(onStateChanged);
    iframe.addEventListener("load", onLoad);
    onLoad();
    const checkSizeHandle = window.setInterval(requestSize, checkSizeInterval);
    const layoutRequestHandle = window.setTimeout(requestLayout, layoutRequestDelay * 10);

    const getDisplayNames = (): Record<string, string> =>
        Object.fromEntries(
            (getRemoteState()?.properties ?? []).filter((x) => !!x.displayName).map((x) => [x.name, x.displayName]),
        );

    const getDocumentSize = (): Dimensions => {
        const document = getDocument();
        if (document) {
            return { x: document.body.offsetWidth, y: document.body.offsetHeight };
        }

        const size = getRemoteState()?.size;
        return size?.x && size?.y ? size : { x: iframe.offsetWidth, y: iframe.offsetHeight };
    };

    const getNodeBox = (selector: string): ElementBox | null => {
        const node = getDocument()?.querySelector<HTMLElement>(selector);
        if (!node) {
            return null;
        }
        const nodeOffset = offset(node, external);
        return {
            position: { x: nodeOffset.left, y: nodeOffset.top },
            size: { x: node.offsetWidth, y: node.offsetHeight },
        };
    };

    const getPropertyBox = (name: string): ElementBox | null => {
        const document = getDocument();
        if (document) {
            return getNodeBox(propertySelector(name));
        }

        const property = getRemoteState()?.properties.find((x) => x.name === name);
        return property ? toBox(property) : null;
    };

    const hitTestDocument = (document: Document, point: Dimensions): FrameHit => {
        const clickedElement = document.elementFromPoint(point.x, point.y) as HTMLElement;
        if (!clickedElement) {
            return {};
        }

        const nodeOffset = offset(clickedElement, external);
        const hit: FrameHit = {
            node: {
                selector: getCssSelector(clickedElement),
                position: { x: nodeOffset.left, y: nodeOffset.top },
                size: { x: clickedElement.offsetWidth, y: clickedElement.offsetHeight },
            },
        };

        const propertyElement =
            getClosest(clickedElement, "[data-epi-property-name]") || getClosest(clickedElement, "[data-epi-edit]");
        if (!propertyElement) {
            return hit;
        }

        hit.property = {
            name: propertyElement.dataset.epiPropertyName || propertyElement.dataset.epiEdit,
            position: { x: propertyElement.offsetLeft, y: propertyElement.offsetTop },
            size: { x: propertyElement.offsetWidth, y: propertyElement.offsetHeight },
        };

        const blockElement = getClosest(clickedElement, "[data-epi-block-id]");
        if (blockElement) {
            hit.block = {
                id: blockElement.dataset.epiBlockId,
                name: blockElement.dataset.epiContentName,
                position: { x: blockElement.offsetLeft, y: blockElement.offsetTop },
                size: { x: blockElement.offsetWidth, y: blockElement.offsetHeight },
            };
        }
        return hit;
    };

    const hitTestRemote = (point: Dimensions): FrameHit => {
        const state = getRemoteState();
        const property = state && smallestContaining(state.properties, point);
        if (!property) {
            return {};
        }

        const hit: FrameHit = { property: { name: property.name, ...toBox(property) } };
        const block =
            state.blocks.find((x) => x.id === property.parentBlockId) ?? smallestContaining(state.blocks, point);
        if (block) {
            hit.block = { id: block.id, ...toBox(block) };
        }
        return hit;
    };

    const hitTest = (framePoint: Dimensions, documentPoint: Dimensions): FrameHit => {
        const document = getDocument();
        return document ? hitTestDocument(document, framePoint) : hitTestRemote(documentPoint);
    };

    const getScrollContainer = (): HTMLElement => {
        if (external) {
            return iframe.parentElement;
        }

        for (
            let element = iframe.parentElement;
            element && element !== document.body;
            element = element.parentElement
        ) {
            if (scrollableOverflow.test(getComputedStyle(element).overflowY)) {
                return element;
            }
        }
        return iframe.parentElement;
    };

    const scroll = (left: number, top: number) => {
        const container = getScrollContainer();
        if (!isRemote() && (external || container.nodeName === "BODY")) {
            iframe.contentWindow.scrollTo(left, top);
            return;
        }

        container.scrollLeft = left;
        container.scrollTop = top;
    };

    const takeRemoteScreenshot = (): Promise<string> =>
        new Promise((resolve, reject) => {
            const eventId = createRandomId();
            const timeout = window.setTimeout(() => {
                screenshotRequests.delete(eventId);
                reject(new Error("The website did not respond to the screenshot request"));
            }, screenshotTimeout);

            screenshotRequests.set(eventId, {
                resolve: (image) => {
                    window.clearTimeout(timeout);
                    resolve(image);
                },
                reject: (error) => {
                    window.clearTimeout(timeout);
                    reject(error);
                },
            });
            iframe.contentWindow?.postMessage({ id: messages.screenshotRequest, data: { eventId } }, "*");
        });

    const takeScreenshot = (): Promise<string> => {
        const document = getDocument();
        if (!document) {
            return takeRemoteScreenshot();
        }

        return html2canvas(document.body, { allowTaint: true, useCORS: true }).then((canvas) => canvas.toDataURL());
    };

    const subscribe = (listener: () => void) => {
        listeners.add(listener);
        return () => {
            listeners.delete(listener);
        };
    };

    const dispose = () => {
        window.clearInterval(checkSizeHandle);
        window.clearTimeout(layoutRequestHandle);
        stateListeners.delete(onStateChanged);
        iframe.removeEventListener("load", onLoad);
        listeners.clear();
        if (!isRemote()) {
            iframe.contentWindow?.removeEventListener("resize", notify);
        }
    };

    fitRemoteFrame();
    requestSize();

    return {
        iframe,
        isRemote,
        getDisplayNames,
        getDocumentSize,
        getNodeBox,
        getPropertyBox,
        hitTest,
        getScrollContainer,
        scroll,
        takeScreenshot,
        subscribe,
        dispose,
    };
}
