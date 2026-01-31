import Icon from "@mui/material/Icon";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import React from "react";

import { DropDownMenu } from "./drop-down-menu";

interface MenuItem {
    name: string;
    icon?: string;
    onSelected: () => void;
}

interface ContextMenuProps {
    title: string;
    icon: string;
    menuItems?: MenuItem[];
}

export class ContextMenu extends React.Component<ContextMenuProps, any> {
    dropDownMenu: DropDownMenu;

    constructor(props: ContextMenuProps) {
        super(props);
        this.state = {
            title: this.props.title,
        };
    }

    onSelected = (index: number) => {
        const menuItem = this.props.menuItems[index];
        menuItem.onSelected();
        this.setState({ title: menuItem.name });
        this.dropDownMenu.closeMenu();
    };

    render() {
        const list = (
            <List>
                {this.props.menuItems.map((item, index) => (
                    <ListItem key={item.name} onClick={() => this.onSelected(index)} style={{ cursor: "pointer" }}>
                        {item.icon && (
                            <ListItemIcon>
                                <Icon>{item.icon}</Icon>
                            </ListItemIcon>
                        )}
                        <ListItemText primary={item.name} />
                    </ListItem>
                ))}
            </List>
        );

        return (
            <DropDownMenu
                ref={(instance) => {
                    this.dropDownMenu = instance;
                }}
                icon={this.props.icon}
                title={this.state.title}
            >
                {list}
            </DropDownMenu>
        );
    }
}
