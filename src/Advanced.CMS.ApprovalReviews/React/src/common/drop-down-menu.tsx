import "./drop-down-menu.scss";

import Icon from "@mui/material/Icon";
import Menu from "@mui/material/Menu";
import React, { PropsWithChildren, useCallback, useRef, useState } from "react";

interface DropDownMenuProps {
    title?: string;
    icon: string | React.ReactNode;
}

export const DropDownMenu: React.FC<PropsWithChildren<DropDownMenuProps>> = ({ title, icon, children }) => {
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const anchorElementRef = useRef<HTMLDivElement>(null);

    const openMenu = useCallback(() => {
        setIsMenuOpen(true);
    }, []);

    const closeMenu = useCallback(() => {
        setIsMenuOpen(false);
    }, []);

    const iconContent = typeof icon === "string" ? <Icon>{icon}</Icon> : icon;

    return (
        <div className="mdc-menu-surface--anchor" ref={anchorElementRef}>
            <div
                className="menu-button"
                onClick={openMenu}
                title={title}
                style={{ cursor: "pointer", display: "inline-flex", alignItems: "center" }}
            >
                {iconContent}
            </div>
            <Menu
                className="epi-context-menu"
                open={isMenuOpen}
                anchorEl={anchorElementRef.current}
                onClose={closeMenu}
                anchorOrigin={{
                    vertical: "bottom",
                    horizontal: "left",
                }}
            >
                {children}
            </Menu>
        </div>
    );
};
