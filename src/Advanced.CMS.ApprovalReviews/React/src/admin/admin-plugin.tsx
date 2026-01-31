import "./admin-plugin.scss";

import axios from "axios";
import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";

interface ReviewLocation {
    contentLink: string;
    editModeUrl: string;
    serializedReview: string;
}

interface ReviewGroup {
    id: string;
    name: string;
    contentLinks: ReviewLocation[];
}

interface AdminPluginProps {
    controllerUrl: string;
}

function AdminPluginComponent(props: AdminPluginProps) {
    const [currentContentLink, setCurrentContentLink] = useState<string>(null);
    const [currentContentName, setCurrentContentName] = useState<string>(null);
    const [currentEditModeUrl, setCurrentEditModeUrl] = useState<string>(null);
    const [currentJSON, setCurrentJSON] = useState<string>(null);
    const [currentException, setCurrentException] = useState<string>(null);
    const [data, setData] = useState<ReviewGroup[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    const refreshList = () => {
        setLoading(true);
        axios
            .get(`${props.controllerUrl}/GetAll`)
            .then((response) => {
                setData(response.data);
                setLoading(false);
            })
            .catch((error) => {
                console.error("Failed to fetch reviews:", error);
                setLoading(false);
            });
    };

    useEffect(() => {
        refreshList();
    }, []);

    const onDeleteClick = (contentLink: string, onSuccess?: () => void) => {
        if (!confirm("Are you sure you want to remove this review?")) {
            return;
        }

        axios
            .post(`${props.controllerUrl}/DeleteReviewLocation`, {
                contentLink: contentLink,
            })
            .then(() => {
                refreshList();
                if (onSuccess) {
                    onSuccess();
                }
            })
            .catch((error) => {
                alert("Failed to delete review: " + error.message);
            });
    };

    const changeReviewLocation = (reviewLocation: ReviewLocation, contentName: string): void => {
        setCurrentContentLink(reviewLocation.contentLink);
        setCurrentContentName(contentName);
        setCurrentEditModeUrl(reviewLocation.editModeUrl);
        try {
            let parsed = JSON.parse(reviewLocation.serializedReview);
            parsed.forEach((reviewLocation) => {
                try {
                    reviewLocation.data = JSON.parse(reviewLocation.data);
                } catch (ex) {}
            });
            parsed = JSON.stringify(parsed, null, 2);
            setCurrentJSON(parsed);
            setCurrentException(null);
        } catch (ex) {
            setCurrentException(ex.message);
            setCurrentJSON(reviewLocation.serializedReview);
        }
    };

    const hasReviews = data && data.length > 0;

    return (
        <div className="root">
            <h2>Saved PIN Viewer</h2>
            <h6>Click on version numbers to see all saved user comments</h6>
            <div className="reviews-list">
                <div className="list-container">
                    {loading ? (
                        <div className="empty-state">
                            <p>Loading reviews...</p>
                        </div>
                    ) : !hasReviews ? (
                        <div className="empty-state">
                            <svg viewBox="0 0 24 24" fill="currentColor">
                                <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14z" />
                                <path d="M7 10h2v7H7zm4-3h2v10h-2zm4 6h2v4h-2z" />
                            </svg>
                            <p>No reviews found</p>
                        </div>
                    ) : (
                        <ul className="list">
                            {data.map((x) => (
                                <li key={x.id || "[no ContentLink]"}>
                                    <div className="main-link">
                                        <div className="content-name">{x.name || "Unnamed Content"}</div>
                                        <div className="content-id">ID: {x.id || "[no ContentLink]"}</div>
                                    </div>
                                    <ul>
                                        {x.contentLinks.map((c) => (
                                            <li className="row" key={c.contentLink || "[empty]"}>
                                                <a
                                                    title={`View saved JSON for ${c.contentLink} ID`}
                                                    href="#"
                                                    onClick={(e) => {
                                                        e.preventDefault();
                                                        changeReviewLocation(c, x.name);
                                                    }}
                                                >
                                                    {c.contentLink || "[empty]"}
                                                </a>
                                                <a
                                                    className="delete"
                                                    href="#"
                                                    onClick={(e) => {
                                                        e.preventDefault();
                                                        onDeleteClick(c.contentLink);
                                                    }}
                                                >
                                                    Delete
                                                </a>
                                            </li>
                                        ))}
                                    </ul>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
                {currentJSON ? (
                    <div className="details">
                        <div className="details-header">
                            <div className="details-info">
                                <div className="details-content-name">{currentContentName || "Unnamed Content"}</div>
                                <div className="details-version">Version: {currentContentLink}</div>
                            </div>
                            <div className="details-actions">
                                {currentEditModeUrl && (
                                    <a
                                        href={currentEditModeUrl}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="icon-button edit-button"
                                        title="Show in edit mode (opens in new tab)"
                                    >
                                        <svg viewBox="0 0 24 24" fill="currentColor">
                                            <path d="M19 19H5V5h7V3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2v-7h-2v7zM14 3v2h3.59l-9.83 9.83 1.41 1.41L19 6.41V10h2V3h-7z" />
                                        </svg>
                                    </a>
                                )}
                                <button
                                    className="icon-button delete-button"
                                    title="Delete this review"
                                    onClick={() => {
                                        onDeleteClick(currentContentLink, () => {
                                            setCurrentJSON(null);
                                            setCurrentContentLink(null);
                                            setCurrentContentName(null);
                                            setCurrentEditModeUrl(null);
                                        });
                                    }}
                                >
                                    <svg viewBox="0 0 24 24" fill="currentColor">
                                        <path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z" />
                                    </svg>
                                </button>
                            </div>
                        </div>
                        {currentException && (
                            <div className="error-message">
                                <strong>Parse Error:</strong> {currentException}
                            </div>
                        )}
                        <div className="json-container">
                            <pre>{currentJSON}</pre>
                        </div>
                    </div>
                ) : (
                    <div className="details placeholder">
                        <div className="placeholder-content">
                            <svg viewBox="0 0 24 24" fill="currentColor">
                                <path d="M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z" />
                            </svg>
                            <h3>No Version Selected</h3>
                            <p>Click on any version number from the list to view the saved JSON data</p>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

const domNode = document.getElementById("admin-plugin-container");
const root = createRoot(domNode);
root.render(<AdminPluginComponent controllerUrl={domNode.dataset.controllerUrl} />);
