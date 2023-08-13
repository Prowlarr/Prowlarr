import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowButton from 'Components/Table/TableRowButton';
import { icons } from 'Helpers/Props';
import ProtocolLabel from 'Indexer/Index/Table/ProtocolLabel';
import firstCharToUpper from 'Utilities/String/firstCharToUpper';
import translate from 'Utilities/String/translate';
import styles from './SelectIndexerRow.css';

class SelectIndexerRow extends Component {

  //
  // Listeners

  onPress = () => {
    const {
      implementation,
      implementationName,
      name
    } = this.props;

    this.props.onIndexerSelect({ implementation, implementationName, name });
  };

  //
  // Render

  render() {
    const {
      protocol,
      privacy,
      name,
      language,
      description,
      isExistingIndexer
    } = this.props;

    return (
      <TableRowButton onPress={this.onPress}>
        <TableRowCell className={styles.protocol}>
          <ProtocolLabel
            protocol={protocol}
          />
        </TableRowCell>

        <TableRowCell>
          {name}
          {
            isExistingIndexer ?
              <Icon
                className={styles.alreadyExistsIcon}
                name={icons.CHECK_CIRCLE}
                size={15}
                title={translate('IndexerAlreadySetup')}
              /> :
              null
          }
        </TableRowCell>

        <TableRowCell>
          {language}
        </TableRowCell>

        <TableRowCell>
          {description}
        </TableRowCell>

        <TableRowCell>
          {translate(firstCharToUpper(privacy))}
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
  description: PropTypes.string.isRequired,
  implementation: PropTypes.string.isRequired,
  implementationName: PropTypes.string.isRequired,
  onIndexerSelect: PropTypes.func.isRequired,
  isExistingIndexer: PropTypes.bool.isRequired
};

export default SelectIndexerRow;
