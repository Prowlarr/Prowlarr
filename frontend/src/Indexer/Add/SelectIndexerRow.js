import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowButton from 'Components/Table/TableRowButton';
import ProtocolLabel from 'Indexer/Index/Table/ProtocolLabel';

class SelectIndexerRow extends Component {

  //
  // Listeners

  onPress = () => {
    const {
      implementation,
      name
    } = this.props;

    this.props.onIndexerSelect({ implementation, name });
  }

  //
  // Render

  render() {
    const {
      protocol,
      privacy,
      name,
      language
    } = this.props;

    return (
      <TableRowButton onPress={this.onPress}>
        <TableRowCell>
          <ProtocolLabel
            protocol={protocol}
          />
        </TableRowCell>

        <TableRowCell>
          {name}
        </TableRowCell>

        <TableRowCell>
          {language}
        </TableRowCell>

        <TableRowCell>
          {privacy}
        </TableRowCell>
      </TableRowButton>
    );
  }
}

SelectIndexerRow.propTypes = {
  name: PropTypes.string.isRequired,
  protocol: PropTypes.string.isRequired,
  privacy: PropTypes.string.isRequired,
  language: PropTypes.string.isRequired,
  implementation: PropTypes.string.isRequired,
  onIndexerSelect: PropTypes.func.isRequired
};

export default SelectIndexerRow;
