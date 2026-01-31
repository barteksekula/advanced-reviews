import "./drop-down-menu.scss";

import Icon from "@mui/material/Icon";
import Menu from "@mui/material/Menu";
import React, { PropsWithChildren } from "react";

interface DropDownMenuProps {
    title?: string;
    icon: string | React.ReactNode;
}

export class DropDownMenu extends React.Component<PropsWithChildren<DropDownMenuProps>, any> {
    constructor(props: PropsWithChildren<DropDownMenuProps>) {
        super(props);
        this.state = {
            isMenuOpen: false,
            anchorElement: null,
        };
    }

    openMenu = () => {
        this.setState({ isMenuOpen: true });
    };

    closeMenu = () => {
        this.setState({ isMenuOpen: false });
    };

    setAnchorElement = (element) => {
        if (this.state.anchorElement) {
            return;
        }
        this.setState({ anchorElement: element });
    };

    render() {
        const iconContent = typeof this.props.icon === "string" ? <Icon>{this.props.icon}</Icon> : this.props.icon;

        return (
            <div className="mdc-menu-surface--anchor" ref={this.setAnchorElement}>
                <div
                    className="menu-button"
                    onClick={this.openMenu}
                    title={this.props.title}
                    style={{ cursor: "pointer", display: "inline-flex", alignItems: "center" }}
                >
                    {iconContent}
                </div>
                <Menu
                    className="epi-context-menu"
                    open={this.state.isMenuOpen}
                    anchorEl={this.state.anchorElement}
                    onClose={this.closeMenu}
                    anchorOrigin={{
                        vertical: "bottom",
                        horizontal: "left",
                    }}
                >
                    {this.props.children}
                </Menu>
            </div>
        );
    }
}
