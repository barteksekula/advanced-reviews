export default function (node: HTMLElement) {
    const rect = node.getBoundingClientRect();
    const view = node.ownerDocument.defaultView;

    return {
        top: rect.top + view.pageYOffset,
        left: rect.left + view.pageXOffset,
    };
}
